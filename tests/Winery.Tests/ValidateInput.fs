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
        |> validateNewCategory getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given an empty 'name'``() =
        let newCategory = {NewCategory.name=""; description="unique desc"}
        newCategory
        |> validateNewCategory getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given an empty 'descriptionn'``() =
        let newCategory = {NewCategory.name="unique name"; description=""}
        newCategory
        |> validateNewCategory getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given a null value for 'name'``() =
        let newCategory = {NewCategory.name=null; description="unique desc"}
        newCategory
        |> validateNewCategory getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given a null value for 'description'``() =
        let newCategory = {NewCategory.name="unique name"; description=null}
        newCategory
        |> validateNewCategory getSomeCategory
        |> shouldBeError    

    [<Fact>]
    let ``Should return success if given unique 'name' and 'description'``() =
        let newCategory = {NewCategory.name="unique name"; description="unique desc"}
        newCategory
        |> validateNewCategory getNoCategory
        |> shouldBeOk


    ////////////////////////////////
    ///// validate updates   //////
    ///////////////////////////////
    [<Fact>]
    let ``Should return error if trying to update with a category that doesn't exist``() =
        let editCategory = {EditCategory.name=Some "new name"; description=None}
        (CategoryID fakeID, editCategory)
        |> validateUpdateCategory getNoCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with an emtpy 'name'``() =
        let editCategory = {EditCategory.name=Some ""; description=None}
        (CategoryID fakeID, editCategory)
        |> validateUpdateCategory getNoCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with an emtpy 'description'``() =
        let editCategory = {EditCategory.name=None; description=Some ""}
        (CategoryID fakeID, editCategory)
        |> validateUpdateCategory getNoCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with nothing to update``() =
        let editCategory = {EditCategory.name=None; description=None}
        (CategoryID fakeID, editCategory)
        |> validateUpdateCategory getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with an existing category 'name'``() =
        let editCategory = {EditCategory.name=Some "fake name"; description=None}
        let getCategory = function
            | Name (CategoryName _) -> getSomeCategory ()
            | ID (CategoryID _) -> getSomeCategory ()

        (CategoryID fakeID, editCategory)
        |> validateUpdateCategory getCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return success if trying to update with a valid updateInfo 'name or description'``() =
        let editCategory = {EditCategory.name=Some "new name"; description=Some "new desc"}
        let getCategory = function
            | Name (CategoryName _) -> getNoCategory ()
            | ID (CategoryID _) -> getSomeCategory ()

        (CategoryID fakeID, editCategory)
        |> validateUpdateCategory getCategory
        |> shouldBeOk

module WineInputValidation =
    [<Fact>]
    let ``Should return error if given same 'name' and 'description'``() =
        let newWine = {NewWine.name="same name"; description="same name"; price=33m; year=2012; imagePath="img/path"}
        (CategoryID fakeID, newWine)
        |> validateNewWine getSomeCategory getSomeWine
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given an empty 'name'``() =
        let newWine = {NewWine.name=""; description="unique desc"; price=33m; year=2012; imagePath="img/path"}
        (CategoryID fakeID, newWine)
        |> validateNewWine getSomeCategory getSomeWine
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given an empty 'descriptionn'``() =
        let newWine = {NewWine.name="unique name"; description=""; price=33m; year=2012; imagePath="img/path"}
        (CategoryID fakeID, newWine)
        |> validateNewWine getSomeCategory getSomeWine
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given a null value for 'name'``() =
        let newWine = {NewWine.name=null; description="unique desc"; price=33m; year=2012; imagePath="img/path"}
        (CategoryID fakeID, newWine)
        |> validateNewWine getSomeCategory getSomeWine
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given a null value for 'descriptionn'``() =
        let newWine = {NewWine.name="unique name"; description=null; year=2002; price=50m; imagePath="img/path"}
        (CategoryID fakeID, newWine)
        |> validateNewWine getSomeCategory getSomeWine
        |> shouldBeError    

    [<Fact>]
    let ``Should return error if given a year less than '1250'``() =
        let newWine = {NewWine.name="unique name"; description="unique desc"; price=33m; year=1042; imagePath="img/path"}
        (CategoryID fakeID, newWine)
        |> validateNewWine getSomeCategory getSomeWine
        |> shouldBeError

    [<Fact>]
    let ``Should return error if given a price less than or equal to '0'``() =
        let newWine = {NewWine.name="unique name"; description="unique desc"; price=0m; year=1042; imagePath="img/path"}
        (CategoryID fakeID, newWine)
        |> validateNewWine getSomeCategory getSomeWine
        |> shouldBeError

    [<Fact>]
    let ``Should return success if given unique 'name', 'description', a price above '0', and year above '1250'``() =
        let newWine = {NewWine.name="unique name"; description="unique desc"; price=33m; year=2012; imagePath="img/path"}
        (CategoryID fakeID, newWine)
        |> validateNewWine getSomeCategory getNoWine
        |> shouldBeOk


    ////////////////////////////////
    ///// validate updates   //////
    ///////////////////////////////
    [<Fact>]
    let ``Should return error if trying to update with a category that doesn't exist``() =
        let editWine = {EditWine.name=Some "new name"; description=None; year=None; price=None; categoryID=Some fakeID; imagePath=None}
        let getWine = function
            | Name (WineName _) -> getNoWine ()
            | ID (WineID _) -> getSomeWine ()
            
        (WineID fakeID, editWine)
        |> validateUpdateWine getWine getNoCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with a wine that doesn't exist``() =
        let editWine = {EditWine.name=Some "new name"; description=None; year=None; price=None; categoryID=None; imagePath=None}
        
        (WineID fakeID, editWine)
        |> validateUpdateWine getNoWine getNoCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with an emtpy 'name'``() =
        let editWine = {EditWine.name=Some ""; description=None; year=None; price=None; categoryID=None; imagePath=None}
        
        (WineID fakeID, editWine)
        |> validateUpdateWine getNoWine getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with an emtpy 'description'``() =
        let editWine = {EditWine.name=None; description=Some ""; year=None; price=None; categoryID=None; imagePath=None}
        let getWine = function
            | Name (WineName _) -> getNoWine ()
            | ID (WineID _) -> getSomeWine ()
            
        (WineID fakeID, editWine)
        |> validateUpdateWine getWine getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with nothing to update``() =
        let editWine = {EditWine.name=None; description=None; year=None; price=None; categoryID=None; imagePath=None}
        let getWine = function
            | Name (WineName _) -> getNoWine ()
            | ID (WineID _) -> getSomeWine ()

        (WineID fakeID, editWine)
        |> validateUpdateWine getWine getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with 'name' equal to 'description'``() =
        let editWine = {EditWine.name=Some "new name"; description=Some "new name"; year=None; price=None; categoryID=None; imagePath=None}
        let getWine = function
            | Name (WineName _) -> getNoWine ()
            | ID (WineID _) -> getSomeWine ()
            
        (WineID fakeID, editWine)
        |> validateUpdateWine getWine getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with 'year' greater than current year``() =
        let editWine = {EditWine.name=None; description=None; year=Some 2050; price=None; categoryID=None; imagePath=None}
        let getWine = function
            | Name (WineName _) -> getNoWine ()
            | ID (WineID _) -> getSomeWine ()
            
        (WineID fakeID, editWine)
        |> validateUpdateWine getWine getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with 'year' less than 1240``() =
        let editWine = {EditWine.name=None; description=None; year=Some 1239; price=None; categoryID=None; imagePath=None}
        let getWine = function
            | Name (WineName _) -> getNoWine ()
            | ID (WineID _) -> getSomeWine ()
            
        (WineID fakeID, editWine)
        |> validateUpdateWine getWine getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with 'price' not greater than 0``() =
        let editWine = {EditWine.name=None; description=None; year=None; price=Some 0m; categoryID=None; imagePath=None}
        let getWine = function
            | Name (WineName _) -> getNoWine ()
            | ID (WineID _) -> getSomeWine ()
            
        (WineID fakeID, editWine)
        |> validateUpdateWine getWine getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return error if trying to update with 'name' of an existing wine``() =
        let editWine = {EditWine.name=None; description=None; year=None; price=Some 0m; categoryID=None; imagePath=None}
            
        (WineID fakeID, editWine)
        |> validateUpdateWine getSomeWine getSomeCategory
        |> shouldBeError

    [<Fact>]
    let ``Should return success if trying to update with a valid updateInfo 'name or description'``() =
        let editWine = {EditWine.name=Some "new name"; description=None; year=None; price=None; categoryID=None; imagePath=None}
        let getWine = function
            | Name (WineName _) -> getNoWine ()
            | ID (WineID _) -> getSomeWine ()

        (WineID fakeID, editWine)
        |> validateUpdateWine getWine getSomeCategory
        |> shouldBeOk
