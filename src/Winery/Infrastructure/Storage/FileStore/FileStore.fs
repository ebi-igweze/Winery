module Storage.FileStore

open Winery
open System.IO
open FSharp.Data
open Newtonsoft.Json

[<Literal>]
let Path = "./Infrastructure/Storage/FileStore/Store.json"

type Store = JsonProvider<Path> 

let storage () = Store.GetSample ()

/////////////////////////
////  Type Transforms
/////////////////////////
let wineToExistingWine (wine: Store.Wine) = {id = wine.Id; categoryID=wine.CategoryId; name=wine.Name; description=wine.Description; year=wine.Year; price=wine.Price; imagePath=wine.ImagePath }
let categoryToExistingCategory (cat: Store.Category) = { id=cat.Id; name=cat.Name; description=cat.Description; wines=cat.Wines |> (Seq.map wineToExistingWine >> Seq.toList) }
let toDomainCartItem getWine (item: Store.Item) = { User.CartItem.id=item.Id; product= item.ProductId |> getWine |> wineToExistingWine; quantity=(uint16) item.Quantity }
let toDomainCart getWine (cart: Store.Cart) = { userId=cart.UserId; items=cart.Items |> Seq.map (toDomainCartItem getWine) |> Seq.toArray }
let userToExistingUser (user: Store.User) = { id = user.Id; firstName = user.FirstName; lastName = user.LastName; email = user.Email; role = user.Role |> function "admin" -> Administrator | _ -> Customer }


/////////////////////////
////  Queries
/////////////////////////

let private queryCategoryById  = fun (CategoryID catId) -> 
    storage()
    |> fun s -> s.Categories
    |> Seq.where (fun c -> c.Id = catId)
    |> Seq.tryHead

let private queryCategoryByName = fun (CategoryName catName) ->
    storage()
    |> fun s -> s.Categories
    |> Seq.where (fun c -> c.Name = catName)
    |> Seq.tryHead
        
let private queryWines = fun () ->
    storage()
    |> fun s -> s.Categories
    |> Seq.map (fun c -> c.Wines)
    |> Seq.collect id

let private queryWineByName = fun (WineName name) ->
    queryWines () |> Seq.tryFind (fun w -> w.Name = name)

let private queryWineById = fun (WineID wineId) -> 
    queryWines () |> Seq.tryFind (fun w -> w.Id = wineId)
        
let private queryWinesInCategory = fun (catId) ->
    catId
    |> queryCategoryById 
    |> Option.map (fun categories -> categories.Wines )

let private queryWineInCategoryByCriteria = fun categoryId criteria ->
    categoryId
    |> queryWinesInCategory 
    |> Option.bind (Seq.tryFind criteria)

let private queryWineInCategoryById = fun (catId) (WineID wineId) -> 
    queryWineInCategoryByCriteria catId (fun w -> w.Id = wineId)

let private queryWineInCategoryByName = fun (catId) (WineName wineName) ->
    queryWineInCategoryByCriteria catId (fun w -> w.Name = wineName)

let private queryUserCart = fun (userId) ->
    storage() 
    |> fun s -> s.Carts
    |> Seq.tryFind (fun c -> c.UserId = userId)

let private queryUserByName = fun (UserName name) ->
    storage()
    |> fun s -> s.Users
    |> Seq.tryFind (fun u -> u.Email = name)

let private queryUserById = fun (UserID id) ->
    storage() 
    |> fun s -> s.Users 
    |> Seq.tryFind (fun u -> u.Id = id)

/////////////////////////
////  Query Stubs
/////////////////////////
let private getCategories = fun () -> 
    storage() 
    |> fun s -> s.Categories 
    |> Seq.map categoryToExistingCategory 
    |> Seq.toList

let private getCategoryById = fun (categoryId) ->
    categoryId |> (queryCategoryById >> Option.map categoryToExistingCategory)

let private getCategoryByName = fun (categoryName) ->
    categoryName |> (queryCategoryByName >> Option.map categoryToExistingCategory)

let private getCategoryByIDorName = function
    | ID categoryId -> getCategoryById categoryId  
    | Name categoryName -> getCategoryByName categoryName

let private getWines = fun () -> queryWines () |> Seq.map wineToExistingWine |> Seq.toList

let private getWineByName = fun (wineName) -> wineName  |> (queryWineByName >> Option.map wineToExistingWine)

let private getWineById = fun (wineId) -> wineId |> (queryWineById >> Option.map wineToExistingWine)

let private getWinesInCategory = fun (catId) -> catId |> (queryWinesInCategory >> Option.map (Seq.map wineToExistingWine) >> Option.map List.ofSeq)

let private getWineInCategoryById = fun catId wineId -> (catId,wineId) ||> queryWineInCategoryById |> Option.map wineToExistingWine 

let private getWineInCategoryByName = fun catId wineName -> (catId, wineName) ||> queryWineInCategoryByName |> Option.map wineToExistingWine

let private getUserCart = fun (UserID userId) -> 
    let getWine id = queryWines () |> Seq.where (fun w -> w.Id = id) |> Seq.head
    userId |> queryUserCart |> Option.map (toDomainCart getWine) 

let private getUserByName = fun (userName) -> userName |> queryUserByName |> Option.map (fun user -> (userToExistingUser user, Password user.Password))

let private getUserById = fun (userId) -> userId |> queryUserById |> Option.map (fun user -> userToExistingUser user, Password user.Password)


/////////////////////////
////  Commands
/////////////////////////
let private getStoreAsString () = File.ReadAllText(Path)



/////////////////////////
////  In Memory Models
/////////////////////////
let categoryQueries: CategoryQueries = 
    { getCategoryById = getCategoryById
      getCategoryByName = getCategoryByName
      getCategories = getCategories }

// let categoryCommandExecutioners: CategoryCommandExecutioners =
//     { addCategory = addCategory
//       updateCategory = updateCategory
//       deleteCategory = removeCategory }

let wineQueries: WineQueries = 
    { getWines = getWines
      getWineById = getWineById
      getWineByName = getWineByName
      getWinesInCategory = getWinesInCategory
      getWineInCategoryByName = getWineInCategoryByName
      getWineInCategoryById = getWineInCategoryById }

// let wineCommandExecutioners: WineCommandExecutioners = 
//     { addWine = addWine
//       updateWine = updateWine
//       deleteWine = removeWine }

let cartQuery: CartQuery = getUserCart
// let cartCommandExecutioner: CartCommandExecutioner = function 
//     | AddItem (userId, cartItem) -> addCartItem (userId, cartItem)
//     | RemoveItem (userId, itemId) -> removeCartItem (userId, itemId)
//     | UpdateQuantity (userId, itemId, quantity) -> updateQuantity (userId, itemId, quantity)

let userQuery: UserQueries = { getUser = function | Name n -> getUserByName n | ID i -> getUserById i }
// let userCommandExecutioners: UserCommandExecutioners = { addUser = addUser; updateUser = updateUser }
