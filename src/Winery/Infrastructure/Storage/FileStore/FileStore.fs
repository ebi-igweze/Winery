module Storage.FileStore

open Winery
open System
open System.IO
open Newtonsoft.Json
open Storage.InMemory
open System.Collections.Generic
open Newtonsoft.Json.Serialization

[<Literal>]
let Path = "./Infrastructure/Storage/FileStore/Store.json"

type private FileStore = InMemoryStore

let private getStoreAsString () = File.ReadAllText(Path)

let jsonSettings = 
    let contractResolver = DefaultContractResolver(NamingStrategy = new CamelCaseNamingStrategy())
    let settings = JsonSerializerSettings(ContractResolver = contractResolver, Formatting = Formatting.Indented)
    settings

let storage = fun () -> JsonConvert.DeserializeObject<FileStore>(getStoreAsString(), jsonSettings)

/////////////////////////
////  Queries
/////////////////////////

let private queryCategoryById (storage: FileStore) = fun (CategoryID catId) -> 
    storage
    |> fun s -> s.categories
    |> Seq.where (fun c -> c.id = catId)
    |> Seq.tryHead

let private queryCategoryByName (storage: FileStore) = fun (CategoryName catName) ->
    storage
    |> fun s -> s.categories
    |> Seq.where (fun c -> c.name = catName)
    |> Seq.tryHead
        
let private queryWines (storage: FileStore) =
    storage
    |> fun s -> s.categories
    |> Seq.map (fun c -> c.wines)
    |> Seq.collect id

let private queryWineByName (storage: FileStore) = fun (WineName name) ->
    queryWines storage |> Seq.tryFind (fun w -> w.name = name)

let private queryWineById (storage: FileStore) = fun (WineID wineId) -> 
    queryWines storage |> Seq.tryFind (fun w -> w.id = wineId)
        
let private queryWinesInCategory (storage: FileStore) = fun (catId) ->
    catId
    |> queryCategoryById storage
    |> Option.map (fun categories -> categories.wines )

let private queryWineInCategoryByCriteria (storage: FileStore) = fun categoryId criteria ->
    categoryId
    |> queryWinesInCategory storage
    |> Option.bind (Seq.tryFind criteria)

let private queryWineInCategoryById (storage: FileStore) = fun (catId) (WineID wineId) -> 
    queryWineInCategoryByCriteria storage catId (fun w -> w.id = wineId)

let private queryWineInCategoryByName (storage: FileStore) = fun (catId) (WineName wineName) ->
    queryWineInCategoryByCriteria storage catId (fun w -> w.name = wineName)

let private queryUserCart (storage: FileStore) = fun (userId) ->
    storage
    |> fun s -> s.carts
    |> Seq.tryFind (fun c -> c.userId = userId)

let private queryUserByName (storage: FileStore) = fun (UserName name) ->
    storage
    |> fun s -> s.users
    |> Seq.tryFind (fun u -> u.email = name)

let private queryUserById (storage: FileStore) = fun (UserID id) ->
    storage
    |> fun s -> s.users 
    |> Seq.tryFind (fun u -> u.id = id)

/////////////////////////
////  Query Stubs
/////////////////////////
let private getCategories (storage: FileStore) = fun () ->
    storage
    |> fun s -> s.categories 
    |> Seq.map categoryToExistingCategory 
    |> Seq.toList

let private getCategoryById (storage: FileStore) = fun (categoryId) ->
    categoryId |> ((queryCategoryById storage) >> Option.map categoryToExistingCategory)

let private getCategoryByName (storage: FileStore) = fun (categoryName) ->
    categoryName |> ((queryCategoryByName storage) >> Option.map categoryToExistingCategory)

let private getWines (storage: FileStore) = fun () -> queryWines storage |> Seq.map wineToExistingWine |> Seq.toList

let private getWineByName (storage: FileStore) = fun (wineName) -> wineName  |> ((queryWineByName storage) >> Option.map wineToExistingWine)

let private getWineById (storage: FileStore) = fun (wineId) -> wineId |> ((queryWineById storage) >> Option.map wineToExistingWine)

let private getWinesInCategory (storage: FileStore) = fun (catId) -> catId |> ((queryWinesInCategory storage) >> Option.map (Seq.map wineToExistingWine) >> Option.map List.ofSeq)

let private getWineInCategoryById (storage: FileStore) = fun catId wineId -> (catId,wineId) ||> (queryWineInCategoryById storage) |> Option.map wineToExistingWine 

let private getWineInCategoryByName (storage: FileStore) = fun catId wineName -> (catId, wineName) ||> (queryWineInCategoryByName storage) |> Option.map wineToExistingWine

let private getUserCart (storage: FileStore) = fun (UserID userId) -> 
    // get the value of the option 
    let getWine = (WineID >> (getWineById storage) >> Option.get)
    userId |> (queryUserCart storage) |> Option.map (toDomainCart getWine) 

let private getUserByName (storage: FileStore) = fun (userName) -> userName |> (queryUserByName storage) |> Option.map (fun user -> (userToExistingUser user, Password user.password))

let private getUserById (storage: FileStore) = fun (userId) -> userId |> (queryUserById storage) |> Option.map (fun user -> userToExistingUser user, Password user.password)


/////////////////////////
////  Commands
/////////////////////////

let saveChanges (store: FileStore) = 
    use streamWriter = File.CreateText(Path)
    let serializer = JsonSerializer.Create(jsonSettings)
    do serializer.Serialize(streamWriter, store)
    
let private addCategory = fun (CategoryID id, category) ->
    let storage =  storage()

    (id, category)
    |> newCategoryToCategory 
    |> storage.categories.Add
    |> fun () -> saveChanges storage
    |> fun () -> Ok "Category added successfully."

let private removeCategory =

    let removeCartItems storage (wineIds: Guid list) = 
        for cart in storage.carts do
            cart.items.RemoveAll (fun i -> List.contains i.productId wineIds)
            |> ignore
    
    fun (categoryId) ->
        let storage = storage()

        categoryId
        |> queryCategoryById storage
        |> Option.map (fun category -> 
            category.wines
            |> Seq.map (fun w -> w.id)
            |> List.ofSeq
            |> removeCartItems storage
            |> fun _ -> category)
        |> Option.map storage.categories.Remove
        |> Option.map (fun _ -> saveChanges storage)
        |> function
            | Some _ -> Ok "Category was removed sucessfully."
            | None   -> Error "Unable to remove Category, please try again."

let private updateCategory =

    let update (editCategory: EditCategory) (category: Category) =
        // extremely ugly way
        if (editCategory.name.IsSome) then category.name <- editCategory.name.Value
        if (editCategory.description.IsSome) then category.description <- editCategory.description.Value

    fun (categoryId, editCategory) ->
        let storage = storage () 
        
        categoryId
        |> queryCategoryById storage
        |> Option.map (update editCategory)
        |> Option.map (fun _ -> saveChanges storage)
        |> function
            | Some _ -> Ok "Category was updated sucessfully."
            | None   -> Error "Unable to update Category, please try again."

let private addWine = 

    let add = fun storage (wine:Wine) -> 
        wine.categoryId
        |> ((queryCategoryById storage) << CategoryID) 
        |> Option.map (fun category -> category.wines.Add wine)
        |> Option.map (fun _ -> saveChanges storage)
        |> function
            | Some _ -> Ok "Wine was added sucessfully."
            | None   -> Error "Unable to add Wine, please try again."

    fun (CategoryID catId, WineID id, wine) ->
        let storage = storage ()
        
        (catId, id, wine)
        |> newWineToWine
        |> add storage

let private removeWine = 

    let removeCartItems = fun storage wineId -> 
        for cart in storage.carts do
            cart.items.RemoveAll (fun item -> item.productId = wineId)
            |> ignore

    let remove = fun storage (wine: Wine) -> 
        wine.categoryId
        |> ((queryCategoryById storage) << CategoryID)
        |> Option.map (fun category -> category.wines.Remove wine)
        |> Option.map (fun _ -> removeCartItems storage wine.id)

    fun (wineId) ->
        let storage = storage()

        wineId
        |> queryWineById storage
        |> Option.bind (remove storage)
        |> Option.map (fun _ -> saveChanges storage)
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
        
    fun (wineId, editWine) ->
        let storage = storage()

        wineId
        |> queryWineById storage
        |> Option.map (update editWine)
        |> Option.map (fun _ -> saveChanges storage)
        |> function
            | Some _ -> Ok "Wine was updated sucessfully."
            | None   -> Error "Unable to update Wine, please try again."

let private addCartItem = fun (UserID userId, cartItem) ->
    let storage = storage ()

    userId
    |> queryUserCart storage
    |> function 
         | Some cart -> (cart.items.Add << toCartItem) cartItem
         | None -> cartItem 
                    |> toCartItem 
                    |> fun item -> {userId=userId; items=List<CartItem>([item]) } 
                    |>  storage.carts.Add 
    |> fun () -> Ok "Item added to cart successfully."

let private removeCartItem = fun (UserID userId, ItemID cartItemId) ->
    let storage = storage ()

    userId
    |> queryUserCart storage
    |> Option.bind (fun cart ->
        cart.items
        |> Seq.tryFind (fun item -> item.id = cartItemId)
        |> Option.map cart.items.Remove )
    |> Option.map (fun _ -> saveChanges storage)
    |> function
        | Some _ -> Ok "Item was removed sucessfully."
        | None   -> Error "Unable to remove item, please try again." 

let private updateQuantity = fun (UserID userId, ItemID cartItemId, quantity) ->
    let storage = storage ()

    userId
    |> queryUserCart storage
    |> Option.bind (fun cart ->
        cart.items
        |> Seq.tryFind (fun item -> item.id = cartItemId)
        |> Option.map (fun  item -> item.quantity <- quantity) ) 
    |> Option.map (fun _ -> saveChanges storage)       
    |> function
        | Some _ -> Ok "Item quantity was updated sucessfully."
        | None   -> Error "Unable to update item, please try again."

let private addUser = fun (UserID id, newUser, Password password) ->
    let storage = storage ()

    (id,newUser,password) 
    |> newUserToUser 
    |> storage.users.Add 
    |> fun () -> saveChanges storage
    |> fun () -> Ok "User account was created sucessfully."
            
let private updateUser =

    let update (editUser: EditUser) (user: User) = 
        if (editUser.email.IsSome) then user.email <- editUser.email.Value
        if (editUser.firstName.IsSome) then user.firstName <- editUser.firstName.Value
        if (editUser.lastName.IsSome) then user.lastName <- editUser.lastName.Value

    fun (userId, editUser) ->
        let storage = storage ()

        userId 
        |> queryUserById storage
        |> Option.map (update editUser)  
        |> Option.map (fun _ -> saveChanges storage)      
        |> function
            | Some _ -> Ok "User information update was sucessfully."
            | None   -> Error "Unable to update User information, please try again."


/////////////////////////
////  FileStore Models
/////////////////////////
let categoryQueries: CategoryQueries = 
    { getCategories      = fun arg -> (storage(), arg) ||> getCategories  
      getCategoryById    = fun arg -> (storage(), arg) ||> getCategoryById 
      getCategoryByName  = fun arg -> (storage(), arg) ||> getCategoryByName  }

let categoryCommandExecutioners: CategoryCommandExecutioners =
    { addCategory    = addCategory
      updateCategory = updateCategory
      deleteCategory = removeCategory }

let wineQueries: WineQueries = 
    { getWines                 = fun arg -> (storage(), arg) ||> getWines
      getWineById              = fun arg -> (storage(), arg) ||> getWineById 
      getWineByName            = fun arg -> (storage(), arg) ||> getWineByName
      getWinesInCategory       = fun arg -> (storage(), arg) ||> getWinesInCategory
      getWineInCategoryById    = fun arg -> (storage(), arg) ||> getWineInCategoryById 
      getWineInCategoryByName  = fun arg -> (storage(), arg) ||> getWineInCategoryByName }

let wineCommandExecutioners: WineCommandExecutioners = 
    { addWine     = addWine
      updateWine  = updateWine
      deleteWine  = removeWine }

let cartQuery: CartQuery = getUserCart (storage())
let cartCommandExecutioner: CartCommandExecutioner = function 
    | AddItem        (userId, cartItem)         -> addCartItem (userId, cartItem)
    | RemoveItem     (userId, itemId)           -> removeCartItem (userId, itemId)
    | UpdateQuantity (userId, itemId, quantity) -> updateQuantity (userId, itemId, quantity)

let userQuery: UserQueries = { getUser = function | Name n -> getUserByName (storage()) n | ID i -> getUserById (storage()) i }
let userCommandExecutioners: UserCommandExecutioners = { addUser = addUser; updateUser = updateUser }
