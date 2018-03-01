[<AutoOpen>]
module Http.Helpers

open Giraffe

let notFound s = setStatusCode 404 >=> text s