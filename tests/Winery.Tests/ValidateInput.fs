module Tests.InputValidation

open Xunit
open Winery

///////////////////////////////////////
////  Tests for input validation   ////
///////////////////////////////////////

module CategoryInputValidation =
    [<Fact>]
    let ``Should return error if given same 'name' and 'description'``() =
        let newCategory = {NewCategory.name="same name"; description="same name"}
        newCategory
        |> createWineCategory getCategoryByName addCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given an empty 'name'``() =
        let newCategory = {NewCategory.name=""; description="unique desc"}
        newCategory
        |> createWineCategory getCategoryByName addCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given an empty 'descriptionn'``() =
        let newCategory = {NewCategory.name="unique name"; description=""}
        newCategory
        |> createWineCategory getCategoryByName addCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given a null value for 'name'``() =
        let newCategory = {NewCategory.name=null; description="unique desc"}
        newCategory
        |> createWineCategory getCategoryByName addCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given a null value for 'descriptionn'``() =
        let newCategory = {NewCategory.name="unique name"; description=null}
        newCategory
        |> createWineCategory getCategoryByName addCategory
        |> shouldBeError    

    [<Fact>]
    let ``Should return success if given unique 'name' and 'description'``() =
        let newCategory = {NewCategory.name="unique name"; description="unique desc"}
        newCategory
        |> createWineCategory getCategoryByName addCategory
        |> shouldBeOk


module WineInputValidation =
    [<Fact>]
    let ``Should return error if given same 'name' and 'description'``() =
        let newWine = {NewWine.name="same name"; description="same name"; price=33m; year=2012; categoryID=catID}
        newWine
        |> createWine getCategoryByID getWineByName addWine
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given an empty 'name'``() =
        let newWine = {NewWine.name=""; description="unique desc"; price=33m; year=2012; categoryID=catID}
        newWine
        |> createWine getCategoryByID getWineByName addWine
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given an empty 'descriptionn'``() =
        let newWine = {NewWine.name="unique name"; description=""; price=33m; year=2012; categoryID=catID}
        newWine
        |> createWine getCategoryByID getWineByName addWine
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given a null value for 'name'``() =
        let newWine = {NewWine.name=null; description="unique desc"; price=33m; year=2012; categoryID=catID}
        newWine
        |> createWine getCategoryByID getWineByName addWine
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given a null value for 'descriptionn'``() =
        let newWine = {NewWine.name="unique name"; description=null; year=2002; price=50m; categoryID=catID}
        newWine
        |> createWine getCategoryByID getWineByName addWine
        |> shouldBeError    

    [<Fact>]
    let ``Should return error if given a year less than '1250'``() =
        let newWine = {NewWine.name="unique name"; description="unique desc"; price=33m; year=1042; categoryID=catID}
        newWine
        |> createWine getCategoryByID getWineByName addWine
        |> shouldBeError

    [<Fact>]
    let ``Should return success if given unique 'name', 'description' and year above '1250'``() =
        let newWine = {NewWine.name="unique name"; description="unique desc"; price=33m; year=2012; categoryID=catID}
        newWine
        |> createWine getCategoryByID getWineByName addWine
        |> shouldBeOk