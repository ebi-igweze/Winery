module Http.Wines

open Storage.InMemory
open Giraffe
open Winery
open Microsoft.AspNetCore.Http

let getWines: HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let store = ctx.GetService<Storage> ()
            let response = store.getWines ()
            return! json response next ctx
        }

let getWineById id =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let store = ctx.GetService<Storage> ()
            return! match store.getWineById (WineID id) with
                    | Some w -> json w next ctx
                    | None -> setStatusCode 404 next ctx
        }