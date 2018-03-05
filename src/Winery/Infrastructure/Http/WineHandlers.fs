module Http.Wines

open Storage.Models
open Winery
open Microsoft.AspNetCore.Http
open Giraffe
open System
open Http.Auth

type RouteFormat<'a> = Printf.TextWriterFormat<'a> 

let getWines categoryId: HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let queries = ctx.GetService<WineQueries> ()
            let response = queries.getWinesInCategory (CategoryID categoryId)
            return! match response with
                    | Some w -> json w next ctx
                    | None -> notFound next ctx
        }

let getWine (categoryStringId: string, idString: string) =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let categoryId, id = Guid categoryStringId, Guid idString
            let queries = ctx.GetService<WineQueries> ()
            let response = queries.getWineInCategoryById (CategoryID categoryId) (WineID id)
            return! match response with
                    | Some w -> json w next ctx
                    | None -> notFound next ctx
        }

let getWineWithName (categoryStringId:string) =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let categoryId = Guid categoryStringId 
            let name = ctx.GetQueryStringValue("name") |> function Ok n -> n | _ -> ""
            let queries = ctx.GetService<WineQueries> ()
            let response = queries.getWineInCategoryByName (CategoryID categoryId) (WineName name)
            return! match response with
                    | Some w -> json w next ctx
                    | None -> notFound next ctx
        }

let postWine (categoryStringId: string): HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        task {
            let! newWine = ctx.BindJsonAsync<NewWine>()
            let categoryId = CategoryID (Guid categoryStringId)
            let categoryQueries = ctx.GetService<CategoryQueries>()
            let wineQueries = ctx.GetService<WineQueries>()
            let wineCommands = ctx.GetService<WineCommands>() 
            return! match categoryQueries.getCategoryById categoryId with
                    | None -> notFound next ctx
                    | Some _ ->
                        let addWine = addWineWith categoryQueries.getCategoryById wineQueries.getWineByName wineCommands.addWine
                        match (addWine <| (fakeAdmin, categoryId, newWine)) with
                        | Ok (WineID id) -> createdM (id.ToString("N"))  next ctx
                        | Error e -> handleError e next ctx
        }

let deleteWine (categoryStringId: string, idString: string): HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let categoryId, wineId = Guid categoryStringId, Guid idString
            let categoryQueries = ctx.GetService<CategoryQueries>()
            let wineQueries = ctx.GetService<WineQueries>()
            let wineCommands = ctx.GetService<WineCommands>() 
            return! match categoryQueries.getCategoryById (CategoryID categoryId) with
                    | None -> notFound next ctx
                    | Some _ -> 
                        let removeWine = removeWineWith wineQueries.getWineById wineCommands.deleteWine
                        match (removeWine <| (fakeAdmin, WineID wineId)) with
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
            let categoryQueries = ctx.GetService<CategoryQueries>()
            let wineQueries = ctx.GetService<WineQueries>()
            let wineCommands = ctx.GetService<WineCommands>() 
            return! match categoryQueries.getCategoryById (CategoryID categoryId) with
                    | None -> notFound next ctx
                    | Some _ -> 
                        let getWineByIdOrName =  function
                            | ID wineId -> wineQueries.getWineById wineId
                            | Name wineName -> wineQueries.getWineByName wineName

                        let editWine = 
                            { EditWine.name=hasValue editInfo.name id; description=hasValue editInfo.description id;
                              price=hasValue editInfo.price Decimal.Parse; imagePath=hasValue editInfo.imagePath id;
                              categoryID=hasValue editInfo.categoryID Guid; year=hasValue editInfo.year Int32.Parse }

                        let updateWine = (editWineWith categoryQueries.getCategoryById getWineByIdOrName wineCommands.updateWine)
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
        subRouteCi "/categories" authorizeAdmin >=> choose [
            POST    >=> routeCif "/%s/wines" (authorizeAdminWithArgs << postWine)
            PUT     >=> routeCif "/%s/wines/%s" (authorizeAdminWithArgs << putWine)
            DELETE  >=> routeCif "/%s/wines/%s" (authorizeAdminWithArgs << deleteWine)
        ]
    ])
