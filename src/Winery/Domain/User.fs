[<AutoOpen>]
module Winery.User
open System

type UserRole = 
| Customer
| Administrator

type User =
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
      user: User 
      items: OrderItem [] }


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

let private isExistingCartItem (getCart: UserID -> Cart option): UserOperation<UserID * ItemID, CartItem> =
    fun (userId, ItemID itemId) -> 
        match getCart userId with
        | None -> invalidUserOp "No Cart has been created for user yet"
        | Some c -> 
            let cartItem = c.items |> Seq.tryFind (fun i -> i.id = itemId || i.product.id = itemId)
            match cartItem with
            | None -> invalidUserOp "This item does not exist in cart"
            | Some item ->  Ok item

let updateItemQuantityInCart getCart (updateCart: CartAction -> _ option): UserOperation<UserID * ItemID * uint16, unit> =
    fun (userId, itemId, quantity) ->
        let updateQuantity = 
            Ok
            >> Result.bind (isExistingCartItem getCart)
            >> Result.bind (fun _ -> if quantity < 1us then invalidUserOp "Item quantity cannot be less than 1" else Ok () )
            >> Result.bind (fun _ -> 
                let update = (userId, itemId, quantity)
                match update |> (UpdateQuantity >> updateCart) with
                | None ->  systemError (unableTo "update item quantity") 
                | Some _ -> Ok ())
        updateQuantity (userId, itemId)
        
let addItemToCart getCart (getWine: WineID -> ExistingWine option) (updateCart: CartAction -> _ option): UserOperation<UserID * WineID * uint16, unit> =
    fun (userId, wineId, quantity) ->
        let (WineID wId) = wineId
        let addItem (wine: ExistingWine) = 
            let itemId = ItemID wine.id
            let createAndAddNewItem wine = 
                let newItem = {id=Guid.NewGuid(); quantity=quantity; product=wine;}
                match (userId, newItem) |> (AddItem >> updateCart) with
                | None -> systemError (unableTo "add item")
                | Some _ -> Ok ()
            match isExistingCartItem getCart (userId, itemId) with
            | Ok item -> updateItemQuantityInCart getCart updateCart (userId, itemId, item.quantity + quantity)
            | Error _ -> 
                if (quantity < 1us)
                then invalidUserOp ("Item quantity cannot be less than 1")             
                else createAndAddNewItem wine  

        match getWine wineId with
        | None -> invalidUserOp (sprintf "A wine with id '%O' does not exist" wId)
        | Some wine -> addItem wine
        
let removeItemFromCart getCart (removeFromCart: CartAction -> _ option): UserOperation<UserID * ItemID, unit> =
    fun (userAndItemID) -> 
        let removeItem = 
            Ok
            >> Result.bind (isExistingCartItem getCart)
            >> Result.bind (fun _ -> 
                match userAndItemID |> (RemoveItem >> removeFromCart) with
                | None -> systemError (unableTo "remove cart item")
                | Some _ -> Ok ())
        removeItem userAndItemID


let placeOrder (getCart: UserID -> Cart option) (checkout: UserID -> _ option): UserOperation<UserID, unit> =
    fun (userId) -> 
        match getCart userId with
        | None ->  invalidUserOp "User has no cart"
        | Some c when Array.isEmpty c.items -> invalidUserOp "User has no cart"
        | Some _ -> 
            match checkout userId with
            | None -> systemError (unableTo "checkout items in cart")
            | Some _ -> Ok ()

///////////////////////////////////////////////////
//// Administrator Operations  
///////////////////////////////////////////////////
let toInvalidOp m =  Result.mapError InvalidOp m

let private validateAdmin =
    let isAdmin user = user.role |> function | Administrator -> true | _ -> false
    errorIf (not << isAdmin) (Unauthorized "unauthorized access")

let addCategoryWith getCategory addCategory: UserOperation<User * NewCategory, CategoryID> =
    fun (user, newCategory) ->
        let addWith = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> validateNewCategory getCategory newCategory |> toInvalidOp)
            >> Result.bind (fun catId -> 
                match addCategory (catId, newCategory) with
                | Some _ -> Ok catId
                | None -> systemError (unableTo "add new category") )
        addWith user

let editCategoryWith getCategory updateCategory: UserOperation<User * CategoryID * EditCategory, unit> =
    fun (user, categoryID, editCategory) ->
        let editWith = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> validateUpdateCategory getCategory (categoryID, editCategory) |> toInvalidOp)
            >> Result.bind (fun _ -> 
                match updateCategory (categoryID, editCategory) with
                | Some _ -> Ok ()
                | None -> systemError (unableTo "update category") )
        editWith user

let removeCategoryWith getCategory deleteCategory: UserOperation<User * CategoryID, unit> =
    fun (user, categoryID) ->
        let removeWith =
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> (validateDeleteCategory getCategory categoryID) |>  toInvalidOp)
            >> Result.bind (fun _ ->
                match deleteCategory categoryID with
                | Some _ -> Ok ()
                | None -> systemError (unableTo "delete category") )
        removeWith user

let addWineWith getCategory getWine addWine: UserOperation<User * CategoryID * NewWine, WineID> =
    fun (user, categoryID, newWine) ->
        let addWith = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> validateNewWine getCategory getWine (categoryID, newWine) |> toInvalidOp)
            >> Result.bind (fun wineId ->  
                match addWine (categoryID, wineId, newWine) with
                | Some _ -> Ok wineId 
                | None -> systemError (unableTo "add new wine") )
        addWith user

let removeWineWith getWine deleteWine: UserOperation<User * WineID, unit> =
    fun (user, wineID) ->
        let removeWith = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> Winery.Core.validateDeleteWine getWine wineID |> toInvalidOp)
            >> Result.bind (fun _ -> 
                match deleteWine wineID with
                | Some _ -> Ok ()
                | None -> systemError (unableTo "delete wine") )
        removeWith user

let editWineWith getCategory getWine updateWine: UserOperation<User * WineID * EditWine, unit> =
    fun (user, wineID, editWine) ->
        let editWith =
            Ok
            >> validateAdmin
            >> Result.bind (fun _ ->  Winery.Core.validateUpdateWine getWine getCategory (wineID, editWine) |> toInvalidOp)
            >> Result.bind (fun _ ->
                match updateWine (wineID, editWine) with 
                | Some _ -> Ok ()
                | None -> systemError (unableTo "update wine") )
        editWith user

let editQuantityWith getWine setQuantity: UserOperation<User * WineID * uint16, unit> = 
    fun (user, wineID, quantity) ->
        let editWith = 
            Ok
            >> validateAdmin
            >> Result.bind (fun _ -> updateQuantity getWine (wineID, quantity) |> toInvalidOp)
            >> Result.bind (fun _ ->
                match setQuantity (wineID, quantity) with
                | Some _ -> Ok ()
                | None -> systemError (unableTo "update wine quantity") )
        editWith user