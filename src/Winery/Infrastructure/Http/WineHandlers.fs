module Http.Wines

open Storage.InMemory
open Winery
open Microsoft.AspNetCore.Http
open Giraffe
open System

type RouteFormat<'a> = Printf.TextWriterFormat<'a> 

let getWines categoryId: HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let store = ctx.GetService<Storage> ()
            let response = store.getWinesInCategory (CategoryID categoryId)
            return! match response with
                    | Some w -> json w next ctx
                    | None -> notFound next ctx
        }

let getWine (categoryStringId: string, idString: string) =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let categoryId, id = Guid categoryStringId, Guid idString
            let store = ctx.GetService<Storage> ()
            let response = store.getWineInCategoryById (CategoryID categoryId) (WineID id)
            return! match response with
                    | Some w -> json w next ctx
                    | None -> notFound next ctx
        }

let getWineWithName (categoryStringId:string) =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let categoryId = Guid categoryStringId 
            let name = ctx.GetQueryStringValue("name") |> function Ok n -> n | _ -> ""
            let store = ctx.GetService<Storage> ()
            let response = store.getWineInCategoryByName (CategoryID categoryId) (WineName name)
            return! match response with
                    | Some w -> json w next ctx
                    | None -> notFound next ctx
        }

let postWine (categoryStringId: string): HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        task {
            let! newWine = ctx.BindJsonAsync<NewWine>()
            let categoryId = CategoryID (Guid categoryStringId)
            let store = ctx.GetService<Storage>()
            return! match store.getCategoryById categoryId with
                    | None -> notFound next ctx
                    | Some _ ->
                        let addWine = addWineWith store.getCategoryById store.getWineByName store.addWine
                        match (addWine <| (fakeAdmin, categoryId, newWine)) with
                        | Ok (WineID id) -> createdM (id.ToString("N"))  next ctx
                        | Error e -> handleError e next ctx
        }

let deleteWine (categoryStringId: string, idString: string): HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let categoryId, wineId = Guid categoryStringId, Guid idString
            let store = ctx.GetService<Storage>()
            return! match store.getCategoryById (CategoryID categoryId) with
                    | None -> notFound next ctx
                    | Some _ -> 
                        match (removeWineWith store.getWineById store.deleteWine <| (fakeAdmin, WineID wineId)) with
                        | Ok _ -> noContent next ctx
                        | Error e -> handleError e next ctx 
        }

type EditInfo = 
    { name: string
      description: string
      year: string
      price: string
      imagePath: string
      categoryID: string }

let putWine (categoryStringId: string, idString: string): HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! editInfo = ctx.BindJsonAsync<EditInfo>()
            let categoryId, wineId = Guid categoryStringId, Guid idString
            let store = ctx.GetService<Storage>()
            return! match store.getCategoryById (CategoryID categoryId) with
                    | None -> notFound next ctx
                    | Some _ -> 
                        let getWineByIdOrName =  function
                            | ID wineId -> store.getWineById wineId
                            | Name wineName -> store.getWineByName wineName

                        let editWine = 
                            { EditWine.name=hasValue editInfo.name id; description=hasValue editInfo.description id;
                              price=hasValue editInfo.price Decimal.Parse; imagePath=hasValue editInfo.imagePath id;
                              categoryID=hasValue editInfo.categoryID Guid; year=hasValue editInfo.year Int32.Parse }

                        let updateWine = (editWineWith store.getCategoryById getWineByIdOrName store.updateWine)
                        match (updateWine <| (fakeAdmin, WineID wineId, editWine)) with
                        | Ok _ -> noContent next ctx
                        | Error e -> handleError e next ctx  
        }

let wineHttpHandlers: HttpHandler = 
    (choose [
        GET >=> choose [
            routeCif "/categories/%O/wines" getWines
            routeCif "/categories/%s/wines/search" getWineWithName
            routeCif "/categories/%s/wines/%s" getWine
        ]
        POST    >=> routeCif "/categories/%s/wines" postWine
        PUT     >=> routeCif "/categories/%s/wines/%s" putWine
        DELETE  >=> routeCif "/categories/%s/wines/%s" deleteWine
    ])
