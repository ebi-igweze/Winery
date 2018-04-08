module Storage.DataStore

open Winery
open FSharp.Data.TypeProviders
open System
open System.Data.Linq

type Winery = SqlDataConnection<ConnectionStringName = "WineryDB", LocalSchemaFile = "Storage.dbml", ForceUpdate = false>

type DataStore = Winery.ServiceTypes.SimpleDataContextTypes.Winery

let storage () = Winery.GetDataContext()


/////////////////////////
////  Transforms
/////////////////////////

let wineToExistingWine (wine: Winery.ServiceTypes.Wine) =
    { id = wine.ID;
      categoryID = wine.CategoryID; 
      name = wine.Name; 
      description = wine.Description; 
      year = wine.Year; 
      price = wine.Price; 
      imagePath = wine.ImagePath }

let newWineToWine (categoryID: Guid, id: Guid, newWine: NewWine) = 
    Winery.ServiceTypes.Wine
        ( ID = id,
          CategoryID = categoryID,
          Name = newWine.name,
          Description = newWine.description,
          Year = newWine.year,
          Price = newWine.price, 
          ImagePath = newWine.imagePath )

let categoryToExistingCategory (cat: Winery.ServiceTypes.Category) = 
    { ExistingCategory.id = cat.ID 
      name = cat.Name
      description = cat.Description; 
      wines = cat.Wine |> (Seq.map wineToExistingWine >> Seq.toList) }

let newCategoryToCategory (id: Guid, cat: NewCategory) = 
    Winery.ServiceTypes.Category
        ( ID = id, 
          Name = cat.name, 
          Description = cat.description )

let toCartItem (item: User.CartItem) = 
    Winery.ServiceTypes.CartItem
        ( ID = item.id,
          WineID = item.product.id, 
          Quantity = int item.quantity )

let toDomainCartItem getWine (item: Winery.ServiceTypes.CartItem) = 
    { User.CartItem.id = item.ID; 
      product = getWine item.WineID; 
      quantity = uint16 item.Quantity }

let toDomainCart getWine (cart: Winery.ServiceTypes.Cart) = 
    { User.Cart.userId = cart.UserID; 
      items = cart.CartItem |> Seq.map (toDomainCartItem getWine) |> Seq.toArray }

let userToExistingUser (user: Winery.ServiceTypes.User) = 
    { ExistingUser.id = user.ID; 
      firstName = user.FirstName; 
      lastName = user.LastName; 
      email = user.Email; 
      role = user.Role |> function "admin" -> Administrator | _ -> Customer }

let newUserToUser (id, user: NewUser, password) = 
    Winery.ServiceTypes.User
        ( ID = id,
          Email = user.email, 
          FirstName = user.firstName,
          LastName = user.lastName, 
          Password = password, 
          Role = string user.role  )


/////////////////////////
////  Queries
/////////////////////////

let private queryCategoryById (storage: DataStore) = fun (CategoryID catId) -> 
    storage
    |> fun s -> s.Category
    |> Seq.where (fun c -> c.ID = catId)
    |> Seq.tryHead

let private queryCategoryByName (storage:  DataStore) = fun (CategoryName catName) ->
    storage
    |> fun s -> s.Category
    |> Seq.where (fun c -> c.Name = catName)
    |> Seq.tryHead
        
let private queryWines (storage:  DataStore) =
    storage
    |> fun s -> s.Category
    |> Seq.map (fun c -> c.Wine)
    |> Seq.collect id

let private queryWineByName (storage:  DataStore) = fun (WineName name) ->
    queryWines storage 
    |> Seq.tryFind (fun w -> w.Name = name)

let private queryWineById (storage:  DataStore) = fun (WineID wineId) -> 
    queryWines storage 
    |> Seq.tryFind (fun w -> w.ID = wineId)
        
let private queryWinesInCategory (storage:  DataStore) = fun (catId) ->
    catId
    |> queryCategoryById storage
    |> Option.map (fun category -> category.Wine)

let private queryWineInCategoryByCriteria (storage:  DataStore) = fun categoryId criteria ->
    categoryId
    |> queryWinesInCategory storage
    |> Option.bind (Seq.tryFind criteria)

let private queryWineInCategoryById (storage:  DataStore) = fun (catId) (WineID wineId) -> 
    queryWineInCategoryByCriteria storage catId (fun w -> w.ID = wineId)

let private queryWineInCategoryByName (storage:  DataStore) = fun (catId) (WineName wineName) ->
    queryWineInCategoryByCriteria storage catId (fun w -> w.Name = wineName)

let private queryUserCart (storage:  DataStore) = fun (userId) ->
    storage
    |> fun s -> s.Cart
    |> Seq.tryFind (fun c -> c.UserID = userId)

let private queryUserByName (storage:  DataStore) = fun (UserName name) ->
    storage
    |> fun s -> s.User
    |> Seq.tryFind (fun u -> u.Email = name)

let private queryUserById (storage:  DataStore) = fun (UserID id) ->
    storage
    |> fun s -> s.User 
    |> Seq.tryFind (fun u -> u.ID = id)

/////////////////////////
////  Query Stubs
/////////////////////////
let private getCategories = fun () ->
    storage()
    |> fun s -> s.Category 
    |> Seq.map categoryToExistingCategory 
    |> Seq.toList

let private getCategoryById = fun (categoryId) ->
    categoryId 
    |> queryCategoryById (storage())
    |> Option.map categoryToExistingCategory

let private getCategoryByName = fun (categoryName) ->
    categoryName 
    |> queryCategoryByName (storage()) 
    |> Option.map categoryToExistingCategory

let private getWines = fun () -> 
    queryWines (storage()) 
    |> Seq.map wineToExistingWine
    |> Seq.toList

let private getWineByName = fun (wineName) ->
    wineName
    |> queryWineByName (storage()) 
    |> Option.map wineToExistingWine

let private getWineById = fun (wineId) -> 
    wineId
    |> queryWineById (storage()) 
    |> Option.map wineToExistingWine

let private getWinesInCategory = fun (catId) -> 
    catId 
    |> queryWinesInCategory (storage()) 
    |> Option.map (Seq.map wineToExistingWine) 
    |> Option.map List.ofSeq

let private getWineInCategoryById = fun catId wineId -> 
    (catId,wineId) 
    ||> queryWineInCategoryById (storage())
    |> Option.map wineToExistingWine 

let private getWineInCategoryByName = fun catId wineName -> 
    (catId, wineName) 
    ||> queryWineInCategoryByName (storage())
    |> Option.map wineToExistingWine

let private getUserCart = fun (UserID userId) -> 
    let storage = storage()
    // get the value of the option 
    let getWine = 
        WineID 
        >> (queryWineById storage) 
        >> Option.map wineToExistingWine 
        >> Option.get

    userId 
    |> (queryUserCart storage) 
    |> Option.map (toDomainCart getWine) 

let private getUserByName = fun (userName) -> 
    userName 
    |> queryUserByName (storage()) 
    |> Option.map (fun user -> (userToExistingUser user, Password user.Password))

let private getUserById = fun (userId) -> 
    userId
    |> queryUserById (storage()) 
    |> Option.map (fun user -> userToExistingUser user, Password user.Password)


/////////////////////////
////  Commands
/////////////////////////
    
let private addCategory = fun (CategoryID id, category) ->
    let storage =  storage()

    (id, category)
    |> newCategoryToCategory 
    |> storage.Category.InsertOnSubmit
    |> storage.DataContext.SubmitChanges
    |> fun () -> Ok "Category added successfully."

let private removeCategory = fun (categoryId) ->
    let storage = storage()

    categoryId
    |> queryCategoryById storage
    |> Option.map storage.Category.DeleteOnSubmit
    |> Option.map storage.DataContext.SubmitChanges
    |> function
        | Some _ -> Ok "Category was removed sucessfully."
        | None   -> Error "Unable to remove Category, please try again."

let private updateCategory =

    let update (editCategory: EditCategory) (category: Winery.ServiceTypes.Category) =
        if (editCategory.name.IsSome)        then category.Name <- editCategory.name.Value
        if (editCategory.description.IsSome) then category.Description <- editCategory.description.Value

    fun (categoryId, editCategory) ->
        let storage = storage () 
        
        categoryId
        |> queryCategoryById storage
        |> Option.map (update editCategory)
        |> Option.map (fun _ -> storage)
        |> function
            | Some _ -> Ok "Category was updated sucessfully."
            | None   -> Error "Unable to update Category, please try again."

let private addWine = fun (CategoryID catId, WineID id, wine) ->
        let storage = storage ()
        
        (catId, id, wine)
        |> newWineToWine
        |> storage.Wine.InsertOnSubmit
        |> storage.DataContext.SubmitChanges
        |> fun () -> Ok "Wine was added sucessfully."

let private removeWine = fun (wineId) ->
        let storage = storage()

        wineId
        |> queryWineById storage
        |> Option.map storage.Wine.DeleteOnSubmit
        |> Option.map storage.DataContext.SubmitChanges
        |> function
            | Some _ -> Ok "Wine was deleted sucessfully."
            | None   -> Error "Unable to delete Wine, please try again."

let private updateWine = 

    let update = fun (editWine: EditWine) (wine: Winery.ServiceTypes.Wine) ->
        // extremely ugly way
        if (editWine.name.IsSome) then wine.Name <- editWine.name.Value
        if (editWine.description.IsSome) then wine.Description <- editWine.description.Value
        if (editWine.price.IsSome) then wine.Price <- editWine.price.Value
        if (editWine.year.IsSome) then wine.Year <- editWine.year.Value
        if (editWine.categoryID.IsSome) then wine.CategoryID <- editWine.categoryID.Value
        
    fun (wineId, editWine) ->
        let storage = storage()

        wineId
        |> queryWineById storage
        |> Option.map (update editWine)
        |> Option.map (fun _ -> storage.DataContext.SubmitChanges)
        |> function
            | Some _ -> Ok "Wine was updated sucessfully."
            | None   -> Error "Unable to update Wine, please try again."

let private addCartItem = fun (UserID userId, cartItem) ->
    let storage = storage ()
    let createItems item = 
        let itemList = EntitySet<Winery.ServiceTypes.CartItem>()
        do  itemList.Add item
        itemList

    userId
    |> queryUserCart storage
    |> function 
         | Some cart -> (cart.CartItem.Add << toCartItem) cartItem
         | None -> cartItem 
                    |> toCartItem 
                    |> fun item -> Winery.ServiceTypes.Cart(UserID = userId, CartItem = createItems item)
                    |> storage.Cart.InsertOnSubmit 
    |> fun () -> Ok "Item added to cart successfully."

let private removeCartItem = fun (UserID userId, ItemID cartItemId) ->
    let storage = storage ()

    let cartItemQuery = query {
        for cart in storage.Cart do
            where (cart.UserID = userId)
            for cartItem in cart.CartItem do
                where (cartItem.ID = cartItemId)
                select cartItem
        } 

    cartItemQuery    
    |> Seq.tryHead
    |> Option.map storage.CartItem.DeleteOnSubmit 
    |> Option.map storage.DataContext.SubmitChanges
    |> function
        | Some _ -> Ok "Cart Item was removed sucessfully."
        | None   -> Error "Unable to remove item, please try again." 
    

let private updateQuantity = fun (UserID userId, ItemID cartItemId, quantity) ->
    let storage = storage ()

    let cartItemQuery = query {
        for cart in storage.Cart do
            where (cart.UserID = userId)
            for cartItem in cart.CartItem do
                where (cartItem.ID = cartItemId)
                select cartItem
        } 

    cartItemQuery
    |> Seq.tryHead
    |> Option.map (fun  item -> item.Quantity <- int quantity) 
    |> Option.map storage.DataContext.SubmitChanges       
    |> function
        | Some _ -> Ok "Cart Item quantity was updated sucessfully."
        | None   -> Error "Unable to update item, please try again."

let private addUser = fun (UserID id, newUser, Password password) ->
    let storage = storage ()

    (id,newUser,password) 
    |> newUserToUser 
    |> storage.User.InsertOnSubmit 
    |> storage.DataContext.SubmitChanges
    |> fun () -> Ok "User account was created sucessfully."
            
let private updateUser =

    let update (editUser: EditUser) (user: Winery.ServiceTypes.User) = 
        if (editUser.email.IsSome) then user.Email <- editUser.email.Value
        if (editUser.firstName.IsSome) then user.FirstName <- editUser.firstName.Value
        if (editUser.lastName.IsSome) then user.LastName <- editUser.lastName.Value

    fun (userId, editUser) ->
        let storage = storage ()

        userId 
        |> queryUserById storage
        |> Option.map (update editUser)  
        |> Option.map (fun _ -> storage.DataContext.SubmitChanges)      
        |> function
            | Some _ -> Ok "User information update was sucessfully."
            | None   -> Error "Unable to update User information, please try again."


/////////////////////////
////  DataStore Models
/////////////////////////
let categoryQueries: CategoryQueries = 
    { getCategories      = getCategories  
      getCategoryById    = getCategoryById 
      getCategoryByName  = getCategoryByName  }

let categoryCommandExecutioners: CategoryCommandExecutioners =
    { addCategory    = addCategory
      updateCategory = updateCategory
      deleteCategory = removeCategory }

let wineQueries: WineQueries = 
    { getWines                 = getWines
      getWineById              = getWineById 
      getWineByName            = getWineByName
      getWinesInCategory       = getWinesInCategory
      getWineInCategoryById    = getWineInCategoryById 
      getWineInCategoryByName  = getWineInCategoryByName }

let wineCommandExecutioners: WineCommandExecutioners = 
    { addWine     = addWine
      updateWine  = updateWine
      deleteWine  = removeWine }

let cartQuery: CartQuery = getUserCart
let cartCommandExecutioner: CartCommandExecutioner = function 
    | AddItem        (userId, cartItem)         -> addCartItem (userId, cartItem)
    | RemoveItem     (userId, itemId)           -> removeCartItem (userId, itemId)
    | UpdateQuantity (userId, itemId, quantity) -> updateQuantity (userId, itemId, quantity)

let userQuery: UserQueries = { getUser = function | Name n -> getUserByName n | ID i -> getUserById i }
let userCommandExecutioners: UserCommandExecutioners = { addUser = addUser; updateUser = updateUser }
