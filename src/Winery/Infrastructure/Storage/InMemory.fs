module Storage.InMemory

open Microsoft.FSharp.Core.Operators.Unchecked
open System.Collections.Generic
open Winery
open System

type Storage = 
    { getCategories             : unit -> ExistingCategory list
      getCategoryById           : CategoryID -> ExistingCategory option
      getCategoryByName         : CategoryName -> ExistingCategory option
      addCategory               : CategoryID * NewCategory -> unit option
      updateCategory            : CategoryID * EditCategory -> unit option
      deleteCategory            : CategoryID -> unit option

      getWines                  : unit -> ExistingWine list
      getWineById               : WineID -> ExistingWine option
      getWineByName             : WineName -> ExistingWine option
      getWinesInCategory         : CategoryID -> ExistingWine list option
      getWineInCategoryById     : CategoryID -> WineID -> ExistingWine option
      getWineInCategoryByName   : CategoryID -> WineName -> ExistingWine option
      addWine                   : WineID * NewWine -> unit option
      updateWine                : WineID * EditWine -> unit option
      deleteWine                : WineID -> unit option }

type Wine() =
    member val id = defaultof<Guid> with get, set
    member val name = "" with get, set
    member val description = "" with get, set
    member val year=0 with get, set
    member val price=0m with get, set
    member val categoryId = defaultof<Guid> with get, set
    member val imagePath = "" with get, set

type Category() =
    member val id = defaultof<Guid> with get, set
    member val name = ""  with get, set
    member val description = "" with get, set
    member val wines = List<Wine>() with get, set
    
type InMemoryStore = { categories:  List<Category> }

/////////////////////////
////  Type Transforms
/////////////////////////
let wineToExistingWine (wine: Wine) = {id=wine.id; categoryID=wine.categoryId; name=wine.name; description=wine.description; year=wine.year; price=wine.price; imagePath=wine.imagePath}
let newWineToWine (id: Guid, newWine: NewWine) = Wine(id=id, categoryId=newWine.categoryID, name=newWine.name, description=newWine.description, year=newWine.year, price=newWine.price, imagePath=newWine.imagePath)
let categoryToExistingCategory (cat: Category) = {id=cat.id; name=cat.name; description=cat.description; wines=cat.wines |> (Seq.map wineToExistingWine >> Seq.toList)}
let newCategoryToCategory (id: Guid, cat: NewCategory) = Category(id=id, name=cat.name, description=cat.description)


/////////////////////////
////  Storage Stubs
/////////////////////////
let wineID1 = Guid("2a6c918595d94d8c80a6575f99c2a716")
let wineID2 = Guid("699fb6e489774ab6ae892b7702556eba")
let catID = Guid("4ec87f064d1e41b49342ab1aead1f99d")

let wine1 = Wine(id=wineID1, name="Edone Grand Cuvee Rose", description="A Sample descripition that will be changed", year=2014, price=14.4m, categoryId=catID, imagePath="img/Sparkling/grand-cuvee.jpg")
let wine2 = Wine(id=wineID2, name="Raventós i Blanc de Nit", description="A Sample description that will be changed", year=2012, price=24.4m, categoryId=catID, imagePath="img/Sparkling/grand-cuvee.jpg")

let wines = List<Wine>()
wines.Add(wine1)
wines.Add(wine2)

let category = Category(id=catID, name="Sparkling", description="A very fizzy type of wine, with Champagne as a typical example.", wines=wines)
let categories = List<Category>()
categories.Add(category)

let storage = { categories=categories }


/////////////////////////
////  Queries
/////////////////////////

let queryCategoryById  = fun (CategoryID catId) -> 
    storage.categories
    |> Seq.where (fun c -> c.id = catId)
    |> Seq.tryHead

let queryCategoryByName = fun (CategoryName catName) ->
    storage.categories
    |> Seq.where (fun c -> c.name = catName)
    |> Seq.tryHead

let queryWineByName =
    fun (WineName name) ->
        storage.categories
        |> Seq.map (fun c -> c.wines)
        |> Seq.collect id
        |> Seq.tryFind (fun w -> w.name = name)
        
let private queryWines = 
    fun () ->
        storage.categories
        |> Seq.map(fun c -> c.wines)
        |> Seq.collect id

let private queryWineById =
    fun (WineID wineId) -> 
        queryWines () |> Seq.tryFind (fun w -> w.id = wineId)
        
let private queryWinesInCategory =
    fun (catId) ->
        catId
        |> queryCategoryById 
        |> Option.map (fun categories -> categories.wines )

let private queryWineInCategoryByCriteria categoryId criteria = 
        categoryId
        |> queryWinesInCategory 
        |> function 
            | None -> None
            | Some wines ->  wines |> Seq.tryFind criteria

let private queryWineInCategoryById = 
    fun (catId) (WineID wineId) -> 
        queryWineInCategoryByCriteria catId (fun w -> w.id = wineId)

let private queryWineInCategoryByName = 
    fun (catId) (WineName wineName) ->
        queryWineInCategoryByCriteria catId (fun w -> w.name = wineName)


/////////////////////////
////  Commands
/////////////////////////
let addCategory =
    fun (CategoryID id, category) ->
        (id, category)
        |> newCategoryToCategory 
        |> storage.categories.Add
        |> Some

let removeCategory =
    fun (categoryId) ->
        categoryId
        |> queryCategoryById
        |> Option.map storage.categories.Remove
        |> Option.map ignore

let updateCategory =
    let update (editCategory: EditCategory) (category: Category) =
        // extremely ugly way
        if (editCategory.name.IsSome) then category.name <- editCategory.name.Value
        if (editCategory.description.IsSome) then category.description <- editCategory.description.Value

    fun (categoryId, editCategory) ->
        categoryId
        |> queryCategoryById
        |> Option.map (update editCategory)

let addWine = 
    let add = fun (wine:Wine) -> 
        wine.categoryId
        |> (queryCategoryById << CategoryID) 
        |> Option.map (fun category -> category.wines.Add wine)

    fun (WineID id, wine) ->
        (id, wine)
        |> newWineToWine
        |> add

let removeWine = 
    let remove = fun (wine: Wine) -> 
        wine.categoryId
        |> (queryCategoryById << CategoryID)
        |> Option.map (fun category -> category.wines.Remove wine)
        |> Option.map ignore

    fun (wineId) ->
        wineId
        |> queryWineById
        |> Option.bind remove

let updateWine =
    let update = fun (editWine: EditWine) (wine: Wine) ->
        // extremely ugly way
        if (editWine.name.IsSome) then wine.name <- editWine.name.Value
        if (editWine.description.IsSome) then wine.name <- editWine.name.Value
        if (editWine.price.IsSome) then wine.price <- editWine.price.Value
        if (editWine.year.IsSome) then wine.year <- editWine.year.Value
        if (editWine.categoryID.IsSome) then wine.categoryId <- editWine.categoryID.Value
        
    fun (wineId, editWine) ->
        wineId
        |> queryWineById
        |> Option.map (update editWine)


/////////////////////////
////  Query Stubs
/////////////////////////
let getCategories = fun () -> storage.categories |> Seq.map categoryToExistingCategory |> Seq.toList

let getCategoryById = fun (categoryId) ->
    categoryId |> (queryCategoryById >> Option.map categoryToExistingCategory)

let getCategoryByName = fun (categoryName) ->
    categoryName |> (queryCategoryByName >> Option.map categoryToExistingCategory)

let getCategoryByIDorName = fun (idOrName) ->
    idOrName |> function
    | ID categoryId -> getCategoryById categoryId  
    | Name categoryName -> getCategoryByName categoryName

let getWines = fun () -> queryWines () |> Seq.map wineToExistingWine |> Seq.toList

let getWineByName = fun (wineName) -> wineName  |> (queryWineByName >> Option.map wineToExistingWine)

let getWineById = fun (wineId) -> wineId |> (queryWineById >> Option.map wineToExistingWine)

let getWinesInCategory = fun (catId) -> catId |> (queryWinesInCategory >> Option.map (Seq.map wineToExistingWine) >> Option.map List.ofSeq)

let getWineInCategoryById = fun catId wineId -> (catId,wineId) ||> queryWineInCategoryById |> Option.map wineToExistingWine

let getWineInCategoryByName = fun catId wineName -> (catId, wineName) ||> queryWineInCategoryByName |> Option.map wineToExistingWine

let getWineByIdOrName = fun (idOrName) ->   
    idOrName |> function
    | ID wineId -> getWineById wineId
    | Name wineName -> getWineByName wineName


/////////////////////////
////  In Memory Store
/////////////////////////
let dataStore: Storage = 
    { getCategoryById = getCategoryById
      getCategoryByName = getCategoryByName
      getCategories = getCategories 
      addCategory = addCategory
      updateCategory = updateCategory
      deleteCategory = removeCategory

      getWines = getWines
      getWineById = getWineById
      getWineByName = getWineByName
      getWinesInCategory = getWinesInCategory
      getWineInCategoryByName = getWineInCategoryByName
      getWineInCategoryById = getWineInCategoryById
      addWine = addWine
      updateWine = updateWine
      deleteWine = removeWine }