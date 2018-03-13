[<AutoOpen>]
module Winery.User
open System

type UserRole = 
| Customer
| Administrator
    with override this.ToString () = 
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

type OperationError = 
| Unauthorized of string
| InvalidOp of string
| SystemError of string

type UserOperation<'T, 'U> = 'T -> Result<'U, OperationError>

let invalidUserOp s = s |> (InvalidOp >> Error)
let unauthorizedUserOp s = s |> (Unauthorized >> Error)
let systemError s = s |> (SystemError >> Error)

///////////////////////////////////////////////////
//// Customer Operation  
///////////////////////////////////////////////////
let addUser getUser addUser: UserOperation<NewUser * Password, unit> = 
    fun (user, password) ->
        match getUser (UserName user.email) with
        | Some _ -> invalidUserOp "A user with this username already exists"
        | None -> 
            let userId = Guid.NewGuid()
            addUser (UserID userId, user, password) |> Ok
            
            

///////////////////////////////////////////////////
//// Customer Operation  
///////////////////////////////////////////////////

let private isExistingCartItem (getCart: UserID -> Cart option): UserOperation<UserID * ItemID, CartItem> =
    fun (userId, ItemID itemId) -> 
        match getCart userId with
        | None -> invalidUserOp "No Cart has been created for user yet"
        | Some c -> 
            let cartItem = c.items |> Seq.tryFind (fun i -> i.id = itemId || i.product.id = itemId)
            match cartItem with
            | None -> invalidUserOp "This item does not exist in cart"
            | Some item ->  Ok item

let updateItemQuantityInCart getCart updateCart: UserOperation<UserID * ItemID * uint16, unit> =
    fun (userId, itemId, quantity) ->
        let updateQuantity = 
            Ok
            >> Result.bind (isExistingCartItem getCart)
            >> Result.bind (fun _ -> if quantity < 1us then invalidUserOp "Item quantity cannot be less than 1" else Ok () )
            >> Result.map (fun _ -> (userId, itemId, quantity) |> (UpdateQuantity >> updateCart) )
        updateQuantity (userId, itemId)
        
let addItemToCart getCart getWine updateCart: UserOperation<UserID * WineID * uint16, unit> =
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
        
let removeItemFromCart getCart removeFromCart: UserOperation<UserID * ItemID, unit> =
    fun (userAndItemID) -> 
        let removeItem = 
            Ok
            >> Result.bind (isExistingCartItem getCart)
            >> Result.map (fun _ -> userAndItemID |> (RemoveItem >> removeFromCart) )
        removeItem userAndItemID


let placeOrder getCart checkout: UserOperation<UserID, unit> =
    fun (userId) -> 
        match getCart userId with
        | None ->  invalidUserOp "User has no cart"
        | Some c when Array.isEmpty c.items -> invalidUserOp "User has no cart"
        | Some _ -> checkout userId |> Ok


///////////////////////////////////////////////////
//// Administrator Operations  
///////////////////////////////////////////////////
let toInvalidOp m =  Result.mapError InvalidOp m

let private validateAdmin =
    let isAdmin user = user.role |> function | Administrator -> true | _ -> false
    errorIf (not << isAdmin) (Unauthorized "unauthorized access")

let addCategoryWith getCategory addCategory: UserOperation<ExistingUser * NewCategory, CategoryID> =
    fun (user, newCategory) ->
        let addWith = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> validateNewCategory getCategory newCategory |> toInvalidOp)
            >> Result.map (fun catId -> addCategory (catId, newCategory); catId )
        addWith user

let editCategoryWith getCategory updateCategory: UserOperation<ExistingUser * CategoryID * EditCategory, unit> =
    fun (user, categoryID, editCategory) ->
        let editWith = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> validateUpdateCategory getCategory (categoryID, editCategory) |> toInvalidOp)
            >> Result.map (fun _ ->  updateCategory (categoryID, editCategory) )
        editWith user

let removeCategoryWith getCategory deleteCategory: UserOperation<ExistingUser * CategoryID, unit> =
    fun (user, categoryID) ->
        let removeWith =
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> (validateDeleteCategory getCategory categoryID) |>  toInvalidOp)
            >> Result.map (fun _ -> deleteCategory categoryID )
        removeWith user

let addWineWith getCategory getWine addWine: UserOperation<ExistingUser * CategoryID * NewWine, WineID> =
    fun (user, categoryID, newWine) ->
        let addWith = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> validateNewWine getCategory getWine (categoryID, newWine) |> toInvalidOp)
            >> Result.map (fun wineId -> addWine (categoryID, wineId, newWine); wineId )
        addWith user

let removeWineWith getWine deleteWine: UserOperation<ExistingUser * WineID, unit> =
    fun (user, wineID) ->
        let removeWith = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> Winery.Core.validateDeleteWine getWine wineID |> toInvalidOp)
            >> Result.map (fun _ -> deleteWine wineID )
        removeWith user

let editWineWith getCategory getWine updateWine: UserOperation<ExistingUser * WineID * EditWine, unit> =
    fun (user, wineID, editWine) ->
        let editWith =
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> Winery.Core.validateUpdateWine getWine getCategory (wineID, editWine) |> toInvalidOp)
            >> Result.map (fun _ -> updateWine (wineID, editWine) )
        editWith user

let editQuantityWith getWine setQuantity: UserOperation<ExistingUser * WineID * uint16, unit> = 
    fun (user, wineID, quantity) ->
        let editWith = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> updateQuantity getWine (wineID, quantity) |> toInvalidOp)
            >> Result.map (fun _ -> setQuantity (wineID, quantity) )
        editWith user