[<AutoOpen>]
module Http.Helpers

open Giraffe

let notFound: HttpHandler = setStatusCode 404 >=> text "The requested resource was not found."