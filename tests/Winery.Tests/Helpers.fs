[<AutoOpen>]
module Tests.Helpers
open Xunit

let shouldEqual expected actual = Assert.StrictEqual(expected, actual)

let shouldBeOf expected actual = Assert.IsType(expected, actual)

let assertFail msg = Assert.True(false, msg)

let assertFailf format args =
    let msg = sprintf format args
    Assert.True(false, msg)

let private assertPass msg = Assert.True(true, msg)

let private assertPassf format args =
    let msg = sprintf format args
    Assert.True(true, msg)

let shouldBeError =
    fun (r: Result<'a, 'b>) ->
        match r with
        | Ok m -> assertFailf "Should return 'error' but returned 'success': '%O'" m
        | Error _ -> assertPass "Error returned"

let shouldBeOk =
    fun (r: Result<'a, 'b>) ->
        match r with
        | Ok _ -> assertPass "Success returned"
        | Error m -> assertFailf "Should return success but returned error: '%O'" m