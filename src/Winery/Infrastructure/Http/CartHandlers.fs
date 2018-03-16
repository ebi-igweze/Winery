module Http.Cart

open Microsoft.AspNetCore.Http
open Giraffe
open System
open Storage.Models
open Winery
open Http.Auth
open Services.Models

let getCart userId: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let cartQuery = ctx.GetService<CartQuery>()
            return! match cartQuery (UserID userId) with
                    | Some c -> json c next ctx
                    | None -> 
                        let errMsg = "No cart has been created for current user"
                        notFoundM errMsg next ctx
        }

type CartItemInfo = 
    { productId: Guid
      quantity: uint16 }

let postCartItem userId: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! newItemInfo = ctx.BindJsonAsync<CartItemInfo>()
            let wineQueries = ctx.GetService<WineQueries>()
            let cartQuery = ctx.GetService<CartQuery>()
            let receiver = ctx.GetService<CartCommandReceiver>()
            let addCartItem = addItemToCart cartQuery wineQueries.getWineById receiver
            return! match (addCartItem <| (UserID userId, WineID newItemInfo.productId, newItemInfo.quantity)) with
                    | Ok (CommandID id) -> accepted id next ctx
                    | Error e -> handleError e next ctx
        }

let putCartItem userId: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! updateInfo = ctx.BindJsonAsync<CartItemInfo>()
            let receiver = ctx.GetService<CartCommandReceiver>()
            let query = ctx.GetService<CartQuery>()
            let updateCartItem = updateItemQuantityInCart query receiver
            return! match (updateCartItem <| (UserID userId, ItemID updateInfo.productId, updateInfo.quantity)) with
                    | Ok (CommandID id) -> accepted id next ctx
                    | Error e -> handleError e next ctx
        }

let deleteCartItem (userStringId: string, itemStringId: string): HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let userId, itemId = Guid userStringId, Guid itemStringId
            let receiver = ctx.GetService<CartCommandReceiver>()
            let query = ctx.GetService<CartQuery>()
            let removeItem = removeItemFromCart query receiver 
            return! match (removeItem <| (UserID userId, ItemID itemId)) with
                    | Ok (CommandID id) -> accepted id next ctx
                    | Error e -> handleError e next ctx
        }

let cartHttpHandlers: HttpHandler = 
    (choose [
        GET     >=> routeCif "/users/%O/cart" (authenticateArgs << getCart)
        POST    >=> routeCif "/users/%O/cart" (authenticateArgs << postCartItem)
        PUT     >=> routeCif "/users/%O/cart" (authenticateArgs << putCartItem)
        DELETE  >=> routeCif "/users/%s/cart/%s" (authenticateArgs << deleteCartItem)
    ])