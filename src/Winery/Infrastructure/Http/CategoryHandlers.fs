module Http.Categories

open Giraffe
open Microsoft.AspNetCore.Http
open Winery
open Storage.InMemory

let getCategories: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let store = ctx.GetService<Storage>()
            let response = store.getCategories()
            return! json response next ctx
        }

let getCategory id =
  fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let store = ctx.GetService<Storage>()
            return! match store.getCategoryById (CategoryID id) with
                    | Some c -> json c next ctx
                    | None -> notFound next ctx
        }

let categoryHttpHandlers: HttpHandler = 
    subRouteCi "/categories" 
        (choose [
            GET >=> choose [
                route "" >=> getCategories
                routef "/%O"  getCategory
            ]
        ]) 
