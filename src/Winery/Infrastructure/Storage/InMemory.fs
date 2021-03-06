module Storage.InMemory

open System.Collections.Generic
open Winery
open System
open System.Runtime.Serialization

[<CLIMutable>]
[<DataContract>]
type Wine = 
    { [<DataMember>] mutable id: Guid
      [<DataMember>] mutable name: string
      [<DataMember>] mutable description: string
      [<DataMember>] mutable year: int32
      [<DataMember>] mutable price: decimal
      [<DataMember>] mutable categoryId: Guid
      [<DataMember>] mutable imagePath: string }

[<CLIMutable>]
[<DataContract>]
type Category = 
    { [<DataMember>] mutable id: Guid
      [<DataMember>] mutable name: string
      [<DataMember>] mutable description: string
      [<DataMember>] mutable wines: List<Wine> }

[<CLIMutable>]
[<DataContract>]
type User = 
    { [<DataMember>] mutable id: Guid
      [<DataMember>] mutable email: string
      [<DataMember>] mutable lastName: string
      [<DataMember>] mutable firstName: string
      [<DataMember>] mutable role: string
      [<DataMember>] mutable password: string }

[<CLIMutable>]
[<DataContract>]
type CartItem = { [<DataMember>] mutable id: Guid; [<DataMember>] mutable productId: Guid; [<DataMember>] mutable quantity: uint16 }

[<CLIMutable>]
type Cart = { userId: Guid; items: List<CartItem> }
    
[<CLIMutable>]
type InMemoryStore = { categories: List<Category>; carts: List<Cart>; users: List<User> }

/////////////////////////
////  Type Transforms
/////////////////////////
let wineToExistingWine (wine: Wine) = {id=wine.id; categoryID=wine.categoryId; name=wine.name; description=wine.description; year=wine.year; price=wine.price; imagePath=wine.imagePath }
let newWineToWine (categoryID: Guid, id: Guid, newWine: NewWine) = { Wine.id=id; categoryId=categoryID; name=newWine.name; description=newWine.description; year=newWine.year; price=newWine.price; imagePath=newWine.imagePath }
let categoryToExistingCategory (cat: Category) = { ExistingCategory.id=cat.id; name=cat.name; description=cat.description; wines=cat.wines |> (Seq.map wineToExistingWine >> Seq.toList) }
let newCategoryToCategory (id: Guid, cat: NewCategory) = { Category.id=id; name=cat.name; description=cat.description; wines=List<Wine>() }
let toCartItem (item: User.CartItem) = { CartItem.id=item.id; productId=item.product.id; quantity=item.quantity }
let toDomainCartItem getWine (item: CartItem) = { User.CartItem.id=item.id; product=(getWine item.productId); quantity=item.quantity }
let toDomainCart getWine (cart: Cart) = { User.Cart.userId=cart.userId; items=cart.items |> Seq.map (toDomainCartItem getWine) |> Seq.toArray }
let userToExistingUser (user: User) = { ExistingUser.id = user.id; firstName = user.firstName; lastName = user.lastName; email = user.email; role = user.role |> function "admin" -> Administrator | _ -> Customer }
let newUserToUser (id, user: NewUser, password) = { email = user.email; id = id; firstName = user.firstName; lastName = user.lastName; password = password; role = string user.role }

/////////////////////////
////  Storage Stubs
/////////////////////////
let wineID1 = Guid("2a6c918595d94d8c80a6575f99c2a716")
let wineID2 = Guid("699fb6e489774ab6ae892b7702556eba")
let catID = Guid("4ec87f064d1e41b49342ab1aead1f99d")

let wine1 = { Wine.id=wineID1; name="Edone Grand Cuvee Rose"; description="A Sample descripition that will be changed"; year=2014; price=14.4m; categoryId=catID; imagePath="img/Sparkling/grand-cuvee.jpg" }
let wine2 = { Wine.id=wineID2; name="Raventós i Blanc de Nit"; description="A Sample description that will be changed"; year=2012; price=24.4m; categoryId=catID; imagePath="img/Sparkling/grand-cuvee.jpg" }

// create and add list of wines
let wines = List<Wine>()
do wines.Add(wine1)
do wines.Add(wine2)

// create and add list of categories
let category = { Category.id=catID; name="Sparkling"; description="A very fizzy type of wine with Champagne as a typical example."; wines=wines }
let categories = List<Category>()
do categories.Add(category)

// create list of carts
let carts = List<Cart>()

// create list of users
let admin = {id=catID; email="Admin"; firstName="Admin"; lastName="Admin"; role="admin"; password=BCrypt.Net.BCrypt.HashPassword("Admin")}
let users = List<User>()
users.Add(admin)

// create In-Memory Storage
let storage = { categories=categories; carts=carts; users=users }

/////////////////////////
////  Queries
/////////////////////////

let private queryCategoryById  = fun (CategoryID catId) -> 
    storage.categories
    |> Seq.where (fun c -> c.id = catId)
    |> Seq.tryHead

let private queryCategoryByName = fun (CategoryName catName) ->
    storage.categories
    |> Seq.where (fun c -> c.name = catName)
    |> Seq.tryHead

let private queryWineByName = fun (WineName name) ->
    storage.categories
    |> Seq.map (fun c -> c.wines)
    |> Seq.collect id
    |> Seq.tryFind (fun w -> w.name = name)
        
let private queryWines = fun () ->
    storage.categories
    |> Seq.map(fun c -> c.wines)
    |> Seq.collect id

let private queryWineById = fun (WineID wineId) -> 
    queryWines () |> Seq.tryFind (fun w -> w.id = wineId)
        
let private queryWinesInCategory = fun (catId) ->
    catId
    |> queryCategoryById 
    |> Option.map (fun categories -> categories.wines )

let private queryWineInCategoryByCriteria categoryId criteria = 
        categoryId
        |> queryWinesInCategory 
        |> Option.bind (Seq.tryFind criteria)

let private queryWineInCategoryById = fun (catId) (WineID wineId) -> 
    queryWineInCategoryByCriteria catId (fun w -> w.id = wineId)

let private queryWineInCategoryByName = fun (catId) (WineName wineName) ->
    queryWineInCategoryByCriteria catId (fun w -> w.name = wineName)

let private queryUserCart = fun (userId) ->
    storage.carts |> Seq.tryFind (fun c -> c.userId = userId)

let private queryUserByName = fun (UserName name) ->
    storage.users |> Seq.tryFind (fun u -> u.email = name)

let private queryUserById = fun (UserID id) ->
    storage.users |> Seq.tryFind (fun u -> u.id = id)


/////////////////////////
////  Commands
/////////////////////////
let private addCategory =
    fun (CategoryID id, category) ->
        (id, category)
        |> newCategoryToCategory 
        |> storage.categories.Add
        |> fun () -> Ok "Category added successfully."

let private removeCategory =    
    let removeCartItems (wineIds: Guid list) = 
        for cart in storage.carts do
            cart.items.RemoveAll (fun i -> List.contains i.productId wineIds)
            |> ignore
    
    fun (categoryId) ->
        categoryId
        |> queryCategoryById
        |> Option.map (fun category -> 
            category.wines
            |> Seq.map (fun w -> w.id)
            |> List.ofSeq
            |> removeCartItems
            |> fun _ -> category)
        |> Option.map storage.categories.Remove
        |> function
            | Some _ -> Ok "Category was removed sucessfully."
            | None   -> Error "Unable to remove Category, please try again."

let private updateCategory =
    let update (editCategory: EditCategory) (category: Category) =
        // extremely ugly way
        if (editCategory.name.IsSome) then category.name <- editCategory.name.Value
        if (editCategory.description.IsSome) then category.description <- editCategory.description.Value

    fun (categoryId, editCategory) ->
        categoryId
        |> queryCategoryById
        |> Option.map (update editCategory)
        |> function
            | Some _ -> Ok "Category was updated sucessfully."
            | None   -> Error "Unable to update Category, please try again."

let private addWine = 
    let add = fun (wine:Wine) -> 
        wine.categoryId
        |> (queryCategoryById << CategoryID) 
        |> Option.map (fun category -> category.wines.Add wine)
        |> function
            | Some _ -> Ok "Wine was added sucessfully."
            | None   -> Error "Unable to add Wine, please try again."

    fun (CategoryID catId, WineID id, wine) ->
        (catId, id, wine)
        |> newWineToWine
        |> add

let private removeWine =
    let removeCartItems = fun wineId -> 
        for cart in storage.carts do
            cart.items.RemoveAll (fun item -> item.productId = wineId)
            |> ignore
 
    let remove = fun (wine: Wine) -> 
        wine.categoryId
        |> (queryCategoryById << CategoryID)
        |> Option.map (fun category -> category.wines.Remove wine)
        |> Option.map (fun _ -> removeCartItems wine.id)

    fun (wineId) ->
        wineId
        |> queryWineById
        |> Option.bind remove
        |> function
            | Some _ -> Ok "Wine was removed sucessfully."
            | None   -> Error "Unable to remove Wine, please try again."

let private updateWine =
    let update = fun (editWine: EditWine) (wine: Wine) ->
        // extremely ugly way
        if (editWine.name.IsSome) then wine.name <- editWine.name.Value
        if (editWine.description.IsSome) then wine.description <- editWine.description.Value
        if (editWine.price.IsSome) then wine.price <- editWine.price.Value
        if (editWine.year.IsSome) then wine.year <- editWine.year.Value
        if (editWine.categoryID.IsSome) then wine.categoryId <- editWine.categoryID.Value
        if (editWine.imagePath.IsSome) then wine.imagePath <- editWine.imagePath.Value
        
    fun (wineId, editWine) ->
        wineId
        |> queryWineById
        |> Option.map (update editWine)
        |> function
            | Some _ -> Ok "Wine was updated sucessfully."
            | None   -> Error "Unable to update Wine, please try again."

let private addCartItem =
    fun (UserID userId, cartItem) ->
        userId
        |> queryUserCart
        |> (function 
             | Some cart -> (cart.items.Add << toCartItem) cartItem
             | None ->  cartItem |> toCartItem |> fun item -> {userId=userId; items=List<CartItem>([item]) } |>  storage.carts.Add)
        |> fun () -> Ok "Item added to cart successfully."

let private removeCartItem = 
    fun (UserID userId, ItemID cartItemId) ->
        userId
        |> queryUserCart
        |> Option.bind (fun cart ->
            cart.items
            |> Seq.tryFind (fun item -> item.id = cartItemId)
            |> Option.map cart.items.Remove )
        |> function
            | Some _ -> Ok "Item was removed sucessfully."
            | None   -> Error "Unable to remove item, please try again." 

let private updateQuantity = 
    fun (UserID userId, ItemID cartItemId, quantity) ->
        userId
        |> queryUserCart
        |> Option.bind (fun cart ->
            cart.items
            |> Seq.tryFind (fun item -> item.id = cartItemId)
            |> Option.map (fun  item -> item.quantity <- quantity) )        
        |> function
            | Some _ -> Ok "Item quantity was updated sucessfully."
            | None   -> Error "Unable to update item, please try again."

let private addUser = 
    fun (UserID id, newUser, Password password) ->
        (id,newUser,password) 
        |> newUserToUser 
        |> storage.users.Add 
        |> fun () -> Ok "User account was created sucessfully."
            
let private updateUser =
    let update (editUser: EditUser) (user: User) = 
        if (editUser.email.IsSome) then user.email <- editUser.email.Value
        if (editUser.firstName.IsSome) then user.firstName <- editUser.firstName.Value
        if (editUser.lastName.IsSome) then user.lastName <- editUser.lastName.Value

    fun (userId, editUser) ->
        userId 
        |> queryUserById
        |> Option.map (update editUser)        
        |> function
            | Some _ -> Ok "User information update was sucessfully."
            | None   -> Error "Unable to update User information, please try again."

/////////////////////////
////  Query Stubs
/////////////////////////
let private getCategories = fun () -> storage.categories |> Seq.map categoryToExistingCategory |> Seq.toList

let private getCategoryById = fun (categoryId) ->
    categoryId |> (queryCategoryById >> Option.map categoryToExistingCategory)

let private getCategoryByName = fun (categoryName) ->
    categoryName |> (queryCategoryByName >> Option.map categoryToExistingCategory)

let private getWines = fun () -> queryWines () |> Seq.map wineToExistingWine |> Seq.toList

let private getWineByName = fun (wineName) -> wineName  |> (queryWineByName >> Option.map wineToExistingWine)

let private getWineById = fun (wineId) -> wineId |> (queryWineById >> Option.map wineToExistingWine)

let private getWinesInCategory = fun (catId) -> catId |> (queryWinesInCategory >> Option.map (Seq.map wineToExistingWine) >> Option.map List.ofSeq)

let private getWineInCategoryById = fun catId wineId -> (catId,wineId) ||> queryWineInCategoryById |> Option.map wineToExistingWine

let private getWineInCategoryByName = fun catId wineName -> (catId, wineName) ||> queryWineInCategoryByName |> Option.map wineToExistingWine

let private getUserCart = fun (UserID userId) -> 
    // get the value of the option 
    let getWine = (WineID >> getWineById >> Option.get)
    userId |> queryUserCart |> Option.map (toDomainCart getWine)

let private getUserByName = fun (userName) -> userName |> queryUserByName |> Option.map (fun user -> (userToExistingUser user, Password user.password))

let private getUserById = fun (userId) -> userId |> queryUserById |> Option.map (fun user -> userToExistingUser user, Password user.password)

/////////////////////////
////  In Memory Models
/////////////////////////
let categoryQueries: CategoryQueries = 
    { getCategoryById = getCategoryById
      getCategoryByName = getCategoryByName
      getCategories = getCategories }

let categoryCommandExecutioners: CategoryCommandExecutioners =
    { addCategory = addCategory
      updateCategory = updateCategory
      deleteCategory = removeCategory }

let wineQueries: WineQueries = 
    { getWines = getWines
      getWineById = getWineById
      getWineByName = getWineByName
      getWinesInCategory = getWinesInCategory
      getWineInCategoryByName = getWineInCategoryByName
      getWineInCategoryById = getWineInCategoryById }

let wineCommandExecutioners: WineCommandExecutioners = 
    { addWine = addWine
      updateWine = updateWine
      deleteWine = removeWine }

let cartQuery: CartQuery = getUserCart
let cartCommandExecutioner: CartCommandExecutioner = function 
    | AddItem (userId, cartItem) -> addCartItem (userId, cartItem)
    | RemoveItem (userId, itemId) -> removeCartItem (userId, itemId)
    | UpdateQuantity (userId, itemId, quantity) -> updateQuantity (userId, itemId, quantity)

let userQuery: UserQueries = { getUser = function | Name n -> getUserByName n | ID i -> getUserById i }
let userCommandExecutioners: UserCommandExecutioners = { addUser = addUser; updateUser = updateUser }