module Tests.Core

open Winery
open Microsoft.FSharp.Core.Operators.Unchecked
open System
open System.Collections.Generic
open Xunit

type Wine() =
    member val id = defaultof<Guid> with get, set
    member val name = "" with get, set
    member val description = "" with get, set
    member val year=0 with get, set
    member val price=0. with get, set
    member val categoryId = defaultof<Guid> with get, set

type Category() =
    member val id = defaultof<Guid> with get, set
    member val name = ""  with get, set
    member val description = "" with get, set
    member val wines = List<Wine>() with get, set
    
type Storage = { categories:  List<Category> }

/////////////////////////
////  Type Transforms
/////////////////////////
let wineToWine (wine: Wine) = {id=wine.id; categoryID=wine.categoryId; name=wine.name; description=wine.description; year=wine.year; price=wine.price}
let newWineToWine (id: Guid, newWine: NewWine) = Wine(id=id, categoryId=newWine.categoryID, name=newWine.name, description=newWine.description, year=newWine.year, price=newWine.price)
let categoryToCategory (cat: Category) = {id=cat.id; name=cat.name; description=cat.description; wines=cat.wines |> (Seq.map wineToWine >> Seq.toList)}
let newCategoryToCategory (id: Guid, cat: NewCategory) = Category(id=id, name=cat.name, description=cat.description)


/////////////////////////
////  Storage Stubs
/////////////////////////
let wineID1 = Guid("2a6c918595d94d8c80a6575f99c2a716")
let wineID2 = Guid("699fb6e489774ab6ae892b7702556eba")
let catID = Guid("4ec87f064d1e41b49342ab1aead1f99d")

let wine1 = Wine(id=wineID1, name="Edone Grand Cuvee Rose", year=2014, price=14.4, categoryId=catID)
let wine2 = Wine(id=wineID2, name="Raventós i Blanc de Nit", year=2012, price=24.4, categoryId=catID)

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
let getCategoryI = fun (catId: Guid) -> storage.categories |> Seq.where (fun c -> c.id = catId)
let getCategoryN = fun (catName: string) -> storage.categories |> Seq.where (fun c -> c.name = catName)

let getCategoryById = (getCategoryI >> Seq.tryHead)
let getCategoryByName = (getCategoryN >> Seq.tryHead)

let getWineByName =
    fun (catId: Guid) (name: string) ->
        catId
        |> getCategoryI
        |> Seq.map (fun c -> c.wines)
        |> Seq.collect id
        |> Seq.tryFind (fun w -> w.name = name)
        
let getWineById =
    fun (catId: Guid) (wineId: Guid) -> 
        catId
        |> getCategoryI
        |> Seq.map (fun c -> c.wines)
        |> Seq.collect id
        |> Seq.tryFind (fun w -> w.id = wineId)

/////////////////////////
////  Commands
/////////////////////////
let addCategory =
    fun (id, category) ->
        (id, category)
        |> newCategoryToCategory 
        |> storage.categories.Add
        |> Some

let removeCategory =
    fun (categoryId) ->
        categoryId
        |> getCategoryById
        |> Option.map storage.categories.Remove

let updateCategory =
    let update (editCategory: EditCategory) (category: Category) =
        // extremely ugly way
        if (editCategory.name.IsSome) then category.name <- editCategory.name.Value
        if (editCategory.description.IsSome) then category.description <- editCategory.description.Value

    fun (categoryId, editCategory) ->
        categoryId
        |> getCategoryById
        |> Option.map (update editCategory)


/////////////////////////
////  Tests Stubs
/////////////////////////
let getCategory = (getCategoryByName >> Option.map categoryToCategory)

/////////////////////////
////  Tests
/////////////////////////
module CategoryInputValidation =
    [<Fact>]
    let ``Should return error if given same 'name' and 'description'``() =
        let newCategory = {NewCategory.name="same name"; description="same name"}
        newCategory
        |> createWineCategory getCategory addCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given an empty 'name'``() =
        let newCategory = {NewCategory.name=""; description="unique desc"}
        newCategory
        |> createWineCategory getCategory addCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given an empty 'descriptionn'``() =
        let newCategory = {NewCategory.name="unique name"; description=""}
        newCategory
        |> createWineCategory getCategory addCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given a null value for 'name'``() =
        let newCategory = {NewCategory.name=null; description="unique desc"}
        newCategory
        |> createWineCategory getCategory addCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given a null value for 'descriptionn'``() =
        let newCategory = {NewCategory.name="unique name"; description=null}
        newCategory
        |> createWineCategory getCategory addCategory
        |> shouldBeError    

    [<Fact>]
    let ``Should return success if given unique 'name' and 'description'``() =
        let newCategory = {NewCategory.name="unique name"; description="unique desc"}
        newCategory
        |> createWineCategory getCategory addCategory
        |> shouldBeOk