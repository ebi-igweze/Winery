[<AutoOpen>]
module Winery.Core

open System

type ExistingWine = 
    { id: Guid
      name: string
      description: string
      year: int 
      price: decimal
      imagePath: string
      categoryID: Guid }

type NewWine = 
    { name: string
      description: string
      year: int
      price: decimal
      imagePath: string }

type EditWine = 
    { name: string option
      description: string option
      year: int option 
      price: decimal option
      imagePath: string option
      categoryID: Guid option }

type ExistingCategory = 
    { id: Guid
      name: string
      description: string 
      wines: ExistingWine list }

type NewCategory = 
    { name: string
      description: string }

type EditCategory = 
    { name: string option
      description: string option }

type WineID = WineID of Guid
type WineName = WineName of string
type CategoryID = CategoryID of Guid
type CategoryName = CategoryName of string

type Inventory =
    { id: Guid
      quantity: uint16
      product: ExistingWine }

type IDorName<'T, 'U> = ID of 'T | Name of 'U

type OperationError = 
| NotFound of string
| InvalidOp of string
| Unauthorized of string
| SystemError of string

type Operation<'T,'U> = 'T -> Result<'U, OperationError>

let notFoundOp s = s |> (NotFound >> Error)
let systemError s = s |> (SystemError >> Error)
let invalidUserOp s = s |> (InvalidOp >> Error)
let unauthorizedUserOp s = s |> (Unauthorized >> Error)

let unableTo performAction = sprintf "Unable to %s at the moment, please try again later." performAction
 
let private isEmptyString value = String.Empty = value || " " = value
let private isMaxStringLength length value = String.length value > length
let private isEqual (value1, value2) = value1 = value2

let errorIf condition errorMsg = 
    fun result -> 
        result |> Result.bind (fun arg ->
            if condition arg 
            then Error (InvalidOp errorMsg)
            else Ok arg )

let private validName =
    fun name ->
        let validate = 
            Ok
            >> errorIf isNull "Name is required but not provided" 
            >> errorIf isEmptyString "Name cannot be empty"
            >> errorIf (isMaxStringLength 20) "Name cannot be more than 20 characters"
        validate name

let private validDescription =
    fun description ->
        let validate = 
            Ok
            >> errorIf isNull "Description is required but not provided"
            >> errorIf isEmptyString "Description cannot be empty"
            >> errorIf (isMaxStringLength 124) "Description name cannot be more than 124 characters"
        validate description

let private validND =
    fun (name, desc) ->
        let validate = 
            Ok
            >> Result.bind (fun _ -> validName name)
            >> Result.bind (fun _ -> validDescription desc)
            >> Result.bind (fun _ -> Ok (name,desc))
            >> errorIf isEqual "Name and Description cannot be the same"
        validate ()
    
///////////////////////////////////////////////////
//// Operations For Wine Category  
///////////////////////////////////////////////////

let private isValidCategoryInfo : Operation<NewCategory, NewCategory> = 
    fun (category : NewCategory) ->
        let validateInfo = 
            Ok
            >> Result.bind (fun (c:NewCategory) -> validND (c.name, c.description))
            >> Result.map (fun _ -> category)
        validateInfo category

let private isNotExistingCategory (getCategory: CategoryName -> ExistingCategory option) : Operation<NewCategory, NewCategory> =
    fun (category : NewCategory) -> 
        match (getCategory << CategoryName) category.name with
        | Some _ -> invalidUserOp (sprintf "A category with name '%s' already exists" category.name)
        | None   -> Ok category

let private isExistingCategoryWithId (getCategory: CategoryID -> ExistingCategory option): Operation<CategoryID, ExistingCategory> =
    fun (categoryId: CategoryID) -> 
        match getCategory categoryId with
        | None -> notFoundOp (sprintf "Category with id '%O' does not exist" categoryId)
        | Some category -> Ok category 

let validateNewCategory getCategory: Operation<NewCategory, CategoryID> = 
    fun (newCategory: NewCategory) -> 
        let create = 
            isValidCategoryInfo 
            >> Result.bind (isNotExistingCategory getCategory)
            >> Result.bind (fun _ -> Ok (CategoryID (Guid.NewGuid())))
        create newCategory

let validateDeleteCategory getCategory: Operation<CategoryID, unit> =
    fun (categoryId: CategoryID) ->
        let delete = 
            Ok
            >> Result.bind (isExistingCategoryWithId getCategory)
            >> Result.map ignore
        delete categoryId
            
let validateUpdateCategory (getCategory: IDorName<CategoryID, CategoryName> -> ExistingCategory option): Operation<CategoryID * EditCategory, unit> =
    let validCategoryUpdateInfo: Operation<EditCategory, EditCategory> =
        fun (editCategory: EditCategory) ->
            match editCategory with
            | {name=(Some n); description=(Some d)} -> validND (n,d) |> Result.map (fun _ -> editCategory)            
            | {name=(Some n)} -> validName n |> Result.map (fun _ -> editCategory)
            | {description=(Some d)} -> validDescription d |> Result.map (fun _ -> editCategory)
            | _ -> invalidUserOp "Nothing to update"

    let notExistingCategoryName: Operation<CategoryID * EditCategory, EditCategory> =
        fun (CategoryID id, editCategory: EditCategory) ->
            match editCategory with
            | {name=None} -> Ok editCategory
            | {name=Some editName} -> 
                match (getCategory << Name << CategoryName) editName with
                | Some c when c.id <> id -> Error (InvalidOp (sprintf "A category with this name '%s' already exists" editName))
                | _ -> Ok editCategory

    fun (categoryId, editCategory) -> 
        let update =
            Ok
            >> Result.bind (isExistingCategoryWithId (ID >> getCategory))
            >> Result.bind (fun _ -> validCategoryUpdateInfo editCategory )
            >> Result.bind (fun editC -> notExistingCategoryName (categoryId, editC))
            >> Result.map ignore
        update categoryId   


/////////////////////////////////////////////////
//// Operations For Wine
//////////////////////////////////////////////////         
let private isValidPrice = errorIf (fun p -> p <= 0m) "Wine price must be greater than 0"
let private isValidYear = errorIf (fun y -> y < 1240 || y > DateTime.Now.Year) "Wine year cannot be less than 1240 or greater than current year"

let private isNotExistingWine (getCategory: WineName -> ExistingWine option): Operation<NewWine, NewWine> =
    fun (newWine: NewWine) -> 
        match (getCategory << WineName) newWine.name with
        | None -> Ok newWine
        | Some _ -> invalidUserOp (sprintf "A wine with the name '%s' already Exists" newWine.name)

let private isExistingWineWithId (getWine: WineID -> ExistingWine option): Operation<WineID, ExistingWine> =
    fun (wineId) ->
        match getWine wineId with
        | Some wine -> Ok wine
        | None -> notFoundOp (sprintf "A wine with this %O does not exist" wineId)

let validateNewWine getCategory getWine: Operation<CategoryID * NewWine, WineID> =
    fun (categoryID, newWine) ->
        let add = 
            Ok
            >> Result.bind (isNotExistingWine getWine)
            >> Result.bind (fun _ -> isExistingCategoryWithId getCategory (categoryID))
            >> Result.bind (fun _ -> validND (newWine.name, newWine.description))
            >> Result.bind (fun _ -> isValidPrice (Ok newWine.price))
            >> Result.bind (fun _ -> isValidYear (Ok newWine.year))
            >> Result.bind (fun _ -> Ok (WineID (Guid.NewGuid())))
        add newWine

let validateDeleteWine getWine: Operation<WineID, unit> =
    fun (id: WineID) -> 
        let delete =
            Ok
            >> Result.bind (isExistingWineWithId getWine)
            >> Result.map ignore
        delete id

let validateUpdateWine (getWine: IDorName<WineID, WineName> -> ExistingWine option) getCategory: Operation<WineID * EditWine, unit> =
    let validWineUpdateInfo: Operation<EditWine, EditWine> = 
        let checkYear (editWine: EditWine) =
            match editWine with
            | {year=Some y} -> 
                Ok y
                |> isValidYear  
                |> Result.bind (fun _ -> Ok editWine)
            | _ -> Ok editWine

        let checkDescription (editWine: EditWine)  =
            match editWine with
            | {description=Some d} ->
                Ok d
                |> Result.bind validDescription
                |> Result.bind (fun _ -> Ok editWine)
            | _ -> Ok editWine

        let checkName (editWine: EditWine) =
            match editWine with
            | {name=Some n} -> 
                Ok n 
                |> Result.bind validName
                |> Result.bind (fun _ -> Ok editWine)
            | _ -> Ok editWine

        let checkNameDescription (editWine: EditWine) =
            match editWine with
            | {name=Some n; description=Some d} ->
                Ok (n,d)
                |> Result.bind validND
                |> Result.bind (fun _ -> Ok editWine)
            | _ -> Ok editWine

        let checkCategory (editWine: EditWine) =
            match editWine with
            | {categoryID=Some id} -> 
                (CategoryID id)
                |> isExistingCategoryWithId getCategory 
                |> Result.bind (fun _ -> Ok editWine)
            | _ -> Ok editWine

        let checkPrice (editWine: EditWine) =
            match editWine with
            | {price=Some p} -> Ok p |> isValidPrice |> fun _ -> Ok editWine
            | _ -> Ok editWine

        fun (editWine: EditWine) ->
            match editWine with
            | {name=None; description=None; year=None; categoryID=None} -> invalidUserOp "Nothing to update"
            | _ -> 
                editWine 
                |> checkName 
                |> Result.bind checkYear
                |> Result.bind checkPrice
                |> Result.bind checkCategory
                |> Result.bind checkDescription 
                |> Result.bind checkNameDescription

    let notExistingWineName: Operation<WineID * EditWine, EditWine> =
        fun (WineID id, editWine: EditWine) ->
            match editWine with
            | {name=None} -> Ok editWine
            | {name=Some editName} -> 
                match (getWine << Name << WineName) editName with
                | Some v when v.id <> id -> invalidUserOp (sprintf "A Wine with this name '%s' already exists" editName)
                | _ -> Ok editWine

    fun (id, editWine) ->
        let update =
            Ok
            >> Result.bind (isExistingWineWithId (getWine << ID))
            >> Result.bind (fun _ -> validWineUpdateInfo editWine)
            >> Result.bind (fun ew -> notExistingWineName (id, ew))
            >> Result.map ignore        
        update id
        

/////////////////////////////////////////////////
//// Operations For Inventory
//////////////////////////////////////////////////
let updateQuantity getWine: Operation<WineID * uint16, unit> =
    fun (wineId, _) ->
        let update = 
            Ok
            >> Result.bind (isExistingWineWithId getWine)
            >> Result.map ignore
        update wineId