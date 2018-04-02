[<AutoOpen>]
module Winery.User
open System

type UserRole = 
| Customer
| Administrator with 
    override this.ToString () = 
        match this with
        | Customer -> "customer"
        | Administrator -> "admin"

type NewUser =
    { email: string
      firstName: string
      lastName: string
      role: UserRole }

type EditUser =
    { email: string option
      firstName: string option
      lastName: string option }

type ExistingUser =
    { id: Guid
      email: string
      firstName: string
      lastName: string
      role: UserRole }

type CartItem = 
    { id: Guid
      product: ExistingWine
      quantity: uint16 }

type Cart = 
    { userId: Guid
      items: CartItem [] }

type OrderItem = 
    { id: Guid
      product: ExistingWine
      price: decimal
      quantity: uint16 }

type Order =
    { id: Guid
      user: ExistingUser 
      items: OrderItem [] }

type Password = Password of string
type UserName = UserName of string

type UserID = UserID of Guid
type ItemID = ItemID of Guid

type CartAction = 
| AddItem of UserID * CartItem 
| UpdateQuantity of UserID * ItemID * uint16
| RemoveItem of UserID * ItemID


///////////////////////////////////////////////////
//// Customer Operation  
///////////////////////////////////////////////////
let addUser getUser addUser: Operation<NewUser * Password, _> = 
    fun (user, password) ->
        match getUser (UserName user.email) with
        | Some _ -> invalidUserOp "A user with this username already exists"
        | None -> 
            let userId = Guid.NewGuid()
            addUser (UserID userId, user, password) |> Ok
            
let editUser (getUser: _ -> ExistingUser option) editUserInfo: Operation<UserID * EditUser, _> =
    fun (UserID userId, editInfo) ->
        match (getUser << ID << UserID) userId with
        | None -> notFoundOp "User does not exist."
        | Some _ -> 
            let performEdit () =  Ok (editUserInfo (UserID userId, editInfo))
            match editInfo.email with
            | Some email ->
                match (getUser << Name << UserName) email with
                | Some u when u.id <> userId ->  invalidUserOp "A user with this email already exists"
                | _ -> performEdit ()
            | None -> performEdit ()

///////////////////////////////////////////////////
//// Customer Operation  
///////////////////////////////////////////////////

let private isExistingCartItem (getCart: UserID -> Cart option): Operation<UserID * ItemID, CartItem> =
    fun (userId, ItemID itemId) -> 
        match getCart userId with
        | None -> invalidUserOp "No Cart has been created for user yet"
        | Some c -> 
            let cartItem = c.items |> Seq.tryFind (fun i -> i.id = itemId || i.product.id = itemId)
            match cartItem with
            | None -> notFoundOp "This item does not exist in cart"
            | Some item ->  Ok item

let updateItemQuantityInCart getCart updateCart: Operation<UserID * ItemID * uint16, _> =
    fun (userId, itemId, quantity) ->
        let updateQuantity = 
            Ok
            >> Result.bind (isExistingCartItem getCart)
            >> Result.bind (fun _ -> if quantity < 1us then invalidUserOp "Item quantity cannot be less than 1" else Ok () )
            >> Result.map (fun _ -> (userId, itemId, quantity) |> (UpdateQuantity >> updateCart) )
        updateQuantity (userId, itemId)
        
let addItemToCart getCart getWine updateCart: Operation<UserID * WineID * uint16, _> =
    fun (userId, wineId, quantity) ->
        let (WineID wId) = wineId
        let addItem (wine: ExistingWine) = 
            let itemId = ItemID wine.id

            let createAndAddNewItem wine = 
                let newItem = { id = Guid.NewGuid(); quantity = quantity; product = wine; }
                (userId, newItem) |> (AddItem >> updateCart >> Ok)
                
            match isExistingCartItem getCart (userId, itemId) with
            | Ok item -> updateItemQuantityInCart getCart updateCart (userId, itemId, item.quantity + quantity)
            | Error _ -> 
                if (quantity < 1us)
                then invalidUserOp ("Item quantity cannot be less than 1")             
                else createAndAddNewItem wine  

        match getWine wineId with
        | None -> invalidUserOp (sprintf "A wine with id '%O' does not exist" wId)
        | Some wine -> addItem wine
        
let removeItemFromCart getCart removeFromCart: Operation<UserID * ItemID, _> =
    fun (userAndItemID) -> 
        let removeItem = 
            Ok
            >> Result.bind (isExistingCartItem getCart)
            >> Result.map (fun _ -> userAndItemID |> (RemoveItem >> removeFromCart) )
        removeItem userAndItemID


let placeOrder (getCart: _ -> Cart option) checkout: Operation<UserID, unit> =
    fun (userId) -> 
        match getCart userId with
        | None ->  invalidUserOp "User has no cart"
        | Some c when Array.isEmpty c.items -> invalidUserOp "User has no cart"
        | Some _ -> checkout userId |> Ok


///////////////////////////////////////////////////
//// Administrator Operations  
///////////////////////////////////////////////////

let private validateAdmin =
    let isAdmin user = user.role |> function | Administrator -> true | _ -> false
    errorIf (not << isAdmin) ("unauthorized access")

let addCategory getCategory addCategory: Operation<ExistingUser * NewCategory, _> =
    fun (user, newCategory) ->
        let add = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> validateNewCategory getCategory newCategory)
            >> Result.map (fun catId -> addCategory (catId, newCategory))
        add user

let editCategory getCategory updateCategory: Operation<ExistingUser * CategoryID * EditCategory, _> =
    fun (user, categoryID, editCategory) ->
        let edit = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> validateUpdateCategory getCategory (categoryID, editCategory))
            >> Result.map (fun _ ->  updateCategory (categoryID, editCategory) )
        edit user

let removeCategory getCategory deleteCategory: Operation<ExistingUser * CategoryID, _> =
    fun (user, categoryID) ->
        let remove =
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> (validateDeleteCategory getCategory categoryID))
            >> Result.map (fun _ -> deleteCategory categoryID )
        remove user

let addWine getCategory getWine addWine: Operation<ExistingUser * CategoryID * NewWine, _> =
    fun (user, categoryID, newWine) ->
        let add = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> validateNewWine getCategory getWine (categoryID, newWine))
            >> Result.map (fun wineId -> addWine (categoryID, wineId, newWine) )
        add user

let removeWine getWine deleteWine: Operation<ExistingUser * WineID, _> =
    fun (user, wineID) ->
        let remove = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> validateDeleteWine getWine wineID)
            >> Result.map (fun _ -> deleteWine wineID )
        remove user

let editWine getCategory getWine updateWine: Operation<ExistingUser * WineID * EditWine, _> =
    fun (user, wineID, editWine) ->
        let edit =
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> validateUpdateWine getWine getCategory (wineID, editWine))
            >> Result.map (fun _ -> updateWine (wineID, editWine) )
        edit user

let editQuantity getWine setQuantity: Operation<ExistingUser * WineID * uint16, _> = 
    fun (user, wineID, quantity) ->
        let edit = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> updateQuantity getWine (wineID, quantity) )
            >> Result.map (fun _ -> setQuantity (wineID, quantity) )
        edit user