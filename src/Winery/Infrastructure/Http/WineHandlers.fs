module Http.Wines

open Storage.InMemory
open Winery
open Microsoft.AspNetCore.Http
open Giraffe

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
            let categoryId, id = System.Guid categoryStringId, System.Guid idString
            let store = ctx.GetService<Storage> ()
            let response = store.getWineInCategoryById (CategoryID categoryId) (WineID id)
            return! match response with
                    | Some w -> json w next ctx
                    | None -> notFound next ctx
        }

let getWineWithName (categoryStringId:string) =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let categoryId = System.Guid categoryStringId 
            let name = ctx.GetQueryStringValue("name") |> function Ok n -> n | _ -> ""
            let store = ctx.GetService<Storage> ()
            let response = store.getWineInCategoryByName (CategoryID categoryId) (WineName name)
            return! match response with
                    | Some w -> json w next ctx
                    | None -> notFound next ctx
        }

let addNewWine (categoryStringId: string): HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) -> 
        task {
            let! newWine = ctx.BindJsonAsync<NewWine>()
            let categoryId = System.Guid categoryStringId
            let store = ctx.GetService<Storage>()
            return! match store.getCategoryById (CategoryID categoryId) with
                    | None -> notFound next ctx
                    | Some _ ->
                        match (addWineWith store.getCategoryById store.getWineByName store.addWine <| (fakeAdmin, newWine)) with
                        | Ok (WineID id) -> createdM (id.ToString("N"))  next ctx
                        | Error (Unauthorized _) -> unauthorized next ctx
                        | Error (InvalidOp m) -> badRequestM m next ctx
                        | Error (SystemError m) -> serverError m next ctx
        }



let wineHttpHandlers: HttpHandler = 
    (choose [
        GET >=> choose [
            routeCif "/categories/%O/wines" getWines
            routeCif "/categories/%s/wines/search" getWineWithName
            routeCif "/categories/%s/wines/%s" getWine
        ]
        POST >=> routeCif "/categories/%s/wines" addNewWine
    ])
