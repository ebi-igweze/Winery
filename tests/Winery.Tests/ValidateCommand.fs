module Tests.CommandValidation

open Winery
open Xunit

/////////////////////////////////////////
////  Tests for command validation   ////
/////////////////////////////////////////
type Command = { mutable invoked: bool }

let getCommand () = 
    let result = { invoked = false}
    let command _ =
        result.invoked <- true
        Some ()
    (result,command)

module CustomerCommands =
    ////////////////////////////////////
    ////  Tests for 'AddCartItem'   ////
    ////////////////////////////////////
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'addCartItem' and 'updateCart' when given a WineID that doesn't exist``() =
        let (updateAction, updateCart) = getCommand ()
        let (addAction, addToCart) = getCommand ()
        let actor = function
            | AddItem _ -> addToCart ()
            | UpdateQuantity _ -> updateCart ()
            | _ -> invalidOp "cannot call this method"

        (UserID userID, WineID fakeID, 1us)
        |> addItemToCart getEmptyCart getNoWine actor
        |> shouldBeError

        (not addAction.invoked && not updateAction.invoked)
        |> shouldEqual true
    
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'addCartItem' and 'updateCart' when given a quantity less than 1``() =
        let (updateAction, updateCart) = getCommand ()
        let (addAction, addToCart) = getCommand ()
        let actor = function
            | AddItem _ -> addToCart ()
            | UpdateQuantity _ -> updateCart ()
            | _ -> invalidOp "cannot call this method"

        (UserID userID, WineID fakeID, 0us)
        |> addItemToCart getEmptyCart getNoWine actor
        |> shouldBeError

        (not addAction.invoked && not updateAction.invoked)
        |> shouldEqual true

    [<Fact>]
    let ``Should *Return success, *Invoke 'addCartItem' and *Not-Invoke 'updateCart' when given an item that's not in cart``() =
        let (updateAction, updateCart) = getCommand ()
        let (addAction, addToCart) = getCommand ()
        let actor = function 
            | AddItem _ -> addToCart ()
            | UpdateQuantity _ -> updateCart ()
            |_ -> invalidOp "cannot call this method"

        (UserID userID, WineID fakeID, 1us)
        |> addItemToCart getEmptyCart getSomeWine actor
        |> shouldBeOk

        (addAction.invoked && not updateAction.invoked)
        |> shouldEqual true

    [<Fact>]
    let ``Should *Return success, *Invoke 'addCartItem' and *Not-Invoke 'updateCart' when no cart has been created for user``() =
        let (updateAction, updateCart) = getCommand ()
        let (addAction, addToCart) = getCommand ()
        let actor = function 
            | AddItem _ -> addToCart ()
            | UpdateQuantity _ -> updateCart ()
            |_ -> invalidOp "cannot call this method"

        (UserID userID, WineID fakeID, 1us)
        |> addItemToCart getNoCart getSomeWine actor
        |> shouldBeOk

        (addAction.invoked && not updateAction.invoked)
        |> shouldEqual true

    [<Fact>]
    let ``Should *Return sucess, *Not-Invoke 'addCartItem' and *Invoke 'UpdateCart' when given an item that is in cart``() = 
        let (updateAction, updateCart) = getCommand ()
        let (addAction, addToCart) = getCommand ()
        let actor = function 
            | AddItem _ -> addToCart ()
            | UpdateQuantity _ -> updateCart ()
            |_ -> invalidOp "cannot call this method"

        (UserID userID, WineID fakeID, 3us)
        |> addItemToCart getCartWithItem getSomeWine actor
        |> shouldBeOk

        (updateAction.invoked && not addAction.invoked)
        |> shouldEqual true

    ///////////////////////////////////////
    ////  Tests for 'UpdateCartItem'   ////
    ///////////////////////////////////////
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'UpdateCart' when no cart has been created``() =
        let (updateAction, updateCart) = getCommand ()
        let actor = function 
            | UpdateQuantity _ -> updateCart ()
            |_ -> invalidOp "cannot call this method"

        (UserID userID, ItemID fakeID, 5us)
        |> updateItemQuantityInCart getNoCart actor
        |> shouldBeError

        (not updateAction.invoked) |> shouldEqual true  

    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'UpdateCart' when given an item that is not in cart``() =
        let (updateAction, updateCart) = getCommand ()
        let actor = function 
            | UpdateQuantity _ -> updateCart ()
            |_ -> invalidOp "cannot call this method"

        (UserID userID, ItemID fakeID, 5us)
        |> updateItemQuantityInCart getCartWithItem actor
        |> shouldBeError

        (not updateAction.invoked) |> shouldEqual true  

    [<Fact>]
    let ``Should *Return sucess and *Invoke 'UpdateCart' when given an item that is in cart``() =
        let (updateAction, updateCart) = getCommand ()
        let actor = function 
            | UpdateQuantity _ -> updateCart ()
            |_ -> invalidOp "cannot call this method"

        (UserID userID, ItemID cartItemID, 5us)
        |> updateItemQuantityInCart getCartWithItem actor
        |> shouldBeOk

        (updateAction.invoked) |> shouldEqual true  


    ///////////////////////////////////////
    ////  Tests for 'RemoveCartItem'   ////
    ///////////////////////////////////////
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'RemoveCartItem' when no cart has been created``() =
        let (deleteAction, deleteCartItem) = getCommand ()
        let actor = function 
            | RemoveItem _ -> deleteCartItem ()
            |_ -> invalidOp "cannot call this method"

        (UserID userID, ItemID fakeID)
        |> removeItemFromCart getNoCart actor
        |> shouldBeError

        (not deleteAction.invoked) |> shouldEqual true  

    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'RemoveCartItem' when given an item that is not in cart``() =
        let (deleteAction, deleteCartItem) = getCommand ()
        let actor = function 
            | RemoveItem _ -> deleteCartItem ()
            |_ -> invalidOp "cannot call this method"

        (UserID userID, ItemID fakeID)
        |> removeItemFromCart getCartWithItem actor
        |> shouldBeError

        (not deleteAction.invoked) |> shouldEqual true  

    [<Fact>]
    let ``Should *Return sucess and *Invoke 'RemoveCartItem' when given an item that is in cart``() =
        let (deleteAction, deleteCartItem) = getCommand ()
        let actor = function 
            | RemoveItem _ -> deleteCartItem ()
            |_ -> invalidOp "cannot call this method"

        (UserID userID, ItemID cartItemID)
        |> removeItemFromCart getCartWithItem actor
        |> shouldBeOk

        (deleteAction.invoked) |> shouldEqual true  
        

    ////////////////////////////////////////////
    ////  Tests for 'Checkout-PlaceOrder'   ////
    ////////////////////////////////////////////
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'placeOrder' when user cart has not been created``() =
        let (checkoutAction, checkout) = getCommand ()
        
        (UserID userID)
        |> placeOrder getNoCart checkout
        |> shouldBeError

        not checkoutAction.invoked |> shouldEqual true

    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'placeOrder' when given an empty cart``() =
        let (checkoutAction, checkout) = getCommand ()
        
        (UserID userID)
        |> placeOrder getEmptyCart checkout
        |> shouldBeError

        not checkoutAction.invoked |> shouldEqual true

    [<Fact>]
    let ``Should *Return success and *Invoke 'placeOrder' when given a cart containing items``() =
        let (checkoutAction, checkout) = getCommand ()
        
        (UserID userID)
        |> placeOrder getCartWithItem checkout
        |> shouldBeOk

        checkoutAction.invoked |> shouldEqual true

module AdminCommands =
    let admin = {User.id=userID; firstName=""; lastName=""; email=""; role=Administrator}
    let customer = {User.id=userID; firstName=""; lastName=""; email=""; role=Customer}
    
    ////////////////////////////////////////
    ////  Tests for 'AddWineCategory'   ////
    ////////////////////////////////////////
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'addCategory' when given an unauthorized user``() =
        let (addCommand, addCategory) = getCommand ()
        let newcategory = {NewCategory.name="some unique name"; description="some description"}
        
        (customer, newcategory)
        |> addCategoryWith getSomeCategory addCategory 
        |> shouldBeError

        not addCommand.invoked |> shouldEqual true

    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'addCategory' when given an exiting category name``() =
        let (addCommand, addCategory) = getCommand ()
        let newcategory = {NewCategory.name=existingCategory.name; description="some description"}
        
        (admin, newcategory)
        |> addCategoryWith getSomeCategory addCategory
        |> shouldBeError

        not addCommand.invoked |> shouldEqual true
       
    [<Fact>]
    let ``Should *Return success and *Invoke 'addCategory' when given an authorized user and valid new category``() =
        let (addCommand, addCategory) = getCommand ()
        let newcategory = {NewCategory.name="some unique name"; description="some description"}
        
        (admin, newcategory)
        |> addCategoryWith getNoCategory addCategory
        |> shouldBeOk

        addCommand.invoked |> shouldEqual true
         
    ////////////////////////////////////////////
    ////  Tests for 'DeleteWineCategory'   /////
    ////////////////////////////////////////////
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'DeleteCategory' when given an categoryId That doesn't exist``() =
        let (deleteCommand, deleteCategory) = getCommand ()
        
        (admin, CategoryID fakeID)
        |> removeCategoryWith getNoCategory deleteCategory
        |> shouldBeError

        not deleteCommand.invoked |> shouldEqual true
        
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'DeleteCategory' when given an unauthorized user``() =
        let (deleteCommand, deleteCategory) = getCommand ()
        
        (customer, CategoryID fakeID)
        |> removeCategoryWith getNoCategory deleteCategory
        |> shouldBeError

        not deleteCommand.invoked |> shouldEqual true

    [<Fact>]
    let ``Should *Return success and *Invoke 'DeleteCategory' when given an authorized user and id for an existing category``() =
        let (deleteCommand, deleteCategory) = getCommand ()
        
        (admin, CategoryID fakeID)
        |> removeCategoryWith getSomeCategory deleteCategory
        |> shouldBeOk

        deleteCommand.invoked |> shouldEqual true

    ////////////////////////////////////////////
    ////  Tests for 'UpdateWineCategory'   /////
    ////////////////////////////////////////////
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'UpdateCategory' when given an unauthorized user``() =
        let (updateCommand, updateCategory) = getCommand ()
        let editCategory = {EditCategory.name=Some ""; description=None;}

        (customer, CategoryID fakeID, editCategory)
        |> editCategoryWith getSomeCategory updateCategory
        |> shouldBeError

        not updateCommand.invoked |> shouldEqual true

    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'UpdateCategory' when given an id for a category that doesn't exist``() =
        let (updateCommand, updateCategory) = getCommand ()
        let editCategory = {EditCategory.name=Some "some name"; description=None;}
        
        (admin, CategoryID fakeID, editCategory)
        |> editCategoryWith getNoCategory updateCategory
        |> shouldBeError

        not updateCommand.invoked |> shouldEqual true
    
    [<Fact>]
    let ``Should *Return success and *Invoke 'UpdateCategory' when given an authorized user and id for an existing category``() =
        let (updateCommand, updateCategory) = getCommand ()
        let editCategory = {EditCategory.name=None; description=Some "new desc";}
        
        (admin, CategoryID fakeID, editCategory)
        |> editCategoryWith getSomeCategory updateCategory
        |> shouldBeOk

        updateCommand.invoked |> shouldEqual true
       
    /////////////////////////////////
    ////  Tests for 'AddWine'   /////
    /////////////////////////////////
    let newWine = {NewWine.name="some name";description="some description";price=35m;year=1253;imagePath="path"}

    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'AddWine' when given an unauthorized user``() =
        let (addCommand, addWine) = getCommand ()

        (customer,CategoryID fakeID, newWine)
        |> addWineWith getSomeCategory getNoWine addWine
        |> shouldBeError

        not addCommand.invoked |> shouldEqual true

    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'AddWine' when given an invalid category ID``() =
        let (addCommand, addWine) = getCommand ()        

        (admin,CategoryID fakeID, newWine)
        |> addWineWith getNoCategory getNoWine addWine
        |> shouldBeError

        not addCommand.invoked |> shouldEqual true

    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'AddWine' when given a name for an existing wine``() =
        let (addCommand, addWine) = getCommand ()        

        (admin,CategoryID fakeID, newWine)
        |> addWineWith getSomeCategory getSomeWine addWine
        |> shouldBeError

        not addCommand.invoked |> shouldEqual true

    [<Fact>]
    let ``Should *Return success and *Invoke 'AddWine' when given an authorized user, existing category and new wine info``() =
        let (addCommand, addWine) = getCommand ()        

        (admin,CategoryID fakeID, newWine)
        |> addWineWith getSomeCategory getNoWine addWine
        |> shouldBeOk

        addCommand.invoked |> shouldEqual true


    ////////////////////////////////////
    ////  Tests for 'RemoveWine'   /////
    ////////////////////////////////////
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'RemoveWine' when given an unauthorized user``() =
        let (removeCommand, removeWine) = getCommand ()
       
        (customer, WineID fakeID)
        |> removeWineWith getSomeWine removeWine
        |> shouldBeError

        not removeCommand.invoked |> shouldEqual true
        
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'RemoveWine' when given an id for a wine that doesn't exist``() =
        let (removeCommand, removeWine) = getCommand ()
       
        (admin, WineID fakeID)
        |> removeWineWith getNoWine removeWine
        |> shouldBeError

        not removeCommand.invoked |> shouldEqual true
        
    [<Fact>]
    let ``Should *Return success and *Invoke 'RemoveWine' when given an authorized user and id for an existing wine``() =
        let (removeCommand, removeWine) = getCommand ()
       
        (admin, WineID fakeID)
        |> removeWineWith getSomeWine removeWine
        |> shouldBeOk

        removeCommand.invoked |> shouldEqual true

        
    ////////////////////////////////////
    ////  Tests for 'UpdateWine'   /////
    ////////////////////////////////////
    let editWineName={EditWine.name=Some "new name";price=Some 44m;imagePath=None;year=Some 2014;categoryID=None;description=None}
    let editWinePath={EditWine.name=None;price=None;imagePath=Some "new path";year=Some 2014;categoryID=None;description=None}

    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'UpdateWine' when given an unauthorized user``() =
        let (updateCommand, updateWine) = getCommand ()
       
        (customer, (WineID fakeID), editWineName)
        |> editWineWith getSomeCategory getSomeWine updateWine
        |> shouldBeError

        not updateCommand.invoked |> shouldEqual true
        
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'UpdateWine' when given an id for a wine that doesn't exist``() =
        let (updateCommand, updateWine) = getCommand ()
       
        (admin, (WineID fakeID), editWineName)
        |> editWineWith getSomeCategory getNoWine updateWine
        |> shouldBeError

        not updateCommand.invoked |> shouldEqual true
    
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'UpdateWine' when given a name for a wine that exists``() =
        let (updateCommand, updateWine) = getCommand ()
       
        (admin, (WineID fakeID), editWineName)
        |> editWineWith getNoCategory getSomeWine updateWine
        |> shouldBeError

        not updateCommand.invoked |> shouldEqual true

    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'UpdateWine' when given an id for a category that doesn't exist``() =
        let (updateCommand, updateWine) = getCommand ()
       
        (admin, (WineID fakeID), editWinePath)
        |> editWineWith getNoCategory getNoWine updateWine
        |> shouldBeError

        not updateCommand.invoked |> shouldEqual true
    
    [<Fact>]
    let ``Should *Return success and *Invoke 'UpdateWine' when given an authorized user, valid category and wine id``() =
        let (updateCommand, updateWine) = getCommand ()

        (admin, (WineID fakeID), editWinePath)
        |> editWineWith getSomeCategory getSomeWine updateWine
        |> shouldBeOk

        updateCommand.invoked |> shouldEqual true

       
    /////////////////////////////////////////
    ////  Tests for 'UpdateInventory'   /////
    /////////////////////////////////////////
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'UpdateQuantity' when given an unauthorized user``() =
        let (updateCommand, updateQuantity) = getCommand ()
        
        (customer, (WineID fakeID), 5us)
        |> editQuantityWith getSomeWine updateQuantity
        |> shouldBeError

        not updateCommand.invoked |> shouldEqual true

    
    [<Fact>]
    let ``Should *Return error and *Not-Invoke 'UpdateQuantity' when given an id for a wine that doesn't exist``() =
        let (updateCommand, updateQuantity) = getCommand ()
        
        (admin, (WineID fakeID), 5us)
        |> editQuantityWith getNoWine updateQuantity
        |> shouldBeError

        not updateCommand.invoked |> shouldEqual true

    
    [<Fact>]
    let ``Should *Return success and *Invoke 'UpdateQuantity' when given an authorized user and id for existing wine``() =
        let (updateCommand, updateQuantity) = getCommand ()
        
        (admin, (WineID fakeID), 5us)
        |> editQuantityWith getSomeWine updateQuantity
        |> shouldBeOk

        updateCommand.invoked |> shouldEqual true