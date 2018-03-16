[<AutoOpen>]
module Http.Helpers

open Giraffe
open Winery
open Microsoft.AspNetCore.Http
open System

type EndPoint = { href : string }

let fakeAdmin: ExistingUser = { id=System.Guid(); email=""; firstName=""; lastName=""; role=Administrator }

let created: HttpHandler = setStatusCode 201

let createdM m: HttpHandler = created >=> text m

let accepted (refId : Guid) : HttpHandler = setStatusCode 202 >=> json ({ href = sprintf "/api/commandStatus/%O" refId })

let noContent: HttpHandler = setStatusCode 204

let badRequest: HttpHandler = setStatusCode 400

let badRequestM m: HttpHandler = setStatusCode 400 >=> text m

let unauthorized: HttpHandler = setStatusCode 401

let unauthorizedM m: HttpHandler = unauthorized >=> text m

let notFound: HttpHandler = setStatusCode 404

let notFoundM m: HttpHandler = notFound >=> text m

let serverError m: HttpHandler = setStatusCode 500 >=> text m

let hasValue input  = fun getValue -> if isNull input then None else Some (getValue input)

let handleError error: HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            return! error |> function
            | NotFound m -> notFoundM m next ctx
            | InvalidOp m -> badRequestM m next ctx
            | SystemError m -> serverError m next ctx
            | Unauthorized _ -> unauthorized next ctx
        }