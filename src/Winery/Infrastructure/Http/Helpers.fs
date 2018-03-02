[<AutoOpen>]
module Http.Helpers

open Giraffe
open Winery

let fakeAdmin: User = { id=System.Guid(); email=""; firstName=""; lastName=""; role=Administrator }

let badRequest: HttpHandler = setStatusCode 400

let badRequestM m: HttpHandler = setStatusCode 400 >=> text m

let notFound: HttpHandler = setStatusCode 404

let notFoundM m: HttpHandler = notFound >=> text m

let unauthorized: HttpHandler = setStatusCode 401

let unauthorizedM m: HttpHandler = unauthorized >=> text m

let accepted: HttpHandler = setStatusCode 202

let created: HttpHandler = setStatusCode 201

let createdM m: HttpHandler = created >=> text m

let serverError m: HttpHandler = setStatusCode 500 >=> text m