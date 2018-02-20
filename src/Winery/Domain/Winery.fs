[<AutoOpen>]
module Winery.Core

open System

type ExistingWine = 
    { id: Guid
      name: string
      description: string
      year: int 
      price: float
      categoryID: Guid }

type NewWine = 
    { name: string
      description: string
      year: int
      price: float
      categoryID: Guid }

type EditWine = 
    { name: string option
      description: string option
      year: int option 
      price: float option
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

type Operation<'T,'U> = 'T -> Result<'U,string>

type GuidOrString = Guid of Guid | String of string

let private unableTo performAction = sprintf "Unable to %s at the moment, please try again later." performAction

let private notNull value = isNull value |> not 
let private notEmptyString value = String.Empty <> value
let private maxStringLength length value = String.length value < length
let private notEqual (value1, value2) = value1 <> value2

let private composeError func (errorMsg: string) = 
    fun result -> 
        match result with
        | Error _ -> result
        | Ok arg -> 
            if func arg 
            then Ok arg
            else Error errorMsg


let private validName =
    fun name ->
        let validate = 
            Ok
            >> composeError notNull "Name is required but not provided" 
            >> composeError notEmptyString "Name cannot be empty"
            >> composeError (maxStringLength 20) "Name cannot be more than 10 characters"
        validate name

let private validDescription =
    fun description ->
        let validate = 
            Ok
            >> composeError notNull "Description is required but not provided"
            >> composeError notEmptyString "Description cannot be empty"
            >> composeError (maxStringLength 124) "Description name cannot be more than 124 characters"
        validate description

let private validND =
    fun (name, desc) ->
        let validate = 
            Ok
            >> Result.bind (fun _ -> validName name)
            >> Result.bind (fun _ -> validDescription desc)
            >> Result.bind (fun _ -> Ok (name,desc))
            >> composeError notEqual "Name and Description cannot be the same"
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

let private isNotExistingCategory (getCategory: string -> ExistingCategory option) : Operation<NewCategory, NewCategory> =
    fun (category : NewCategory) -> 
        match getCategory category.name with
        | Some _ -> Error (sprintf "A category with name '%s' already exists" category.name)
        | None   -> Ok category

let isExistingCategoryWithId (getCategory: Guid -> ExistingCategory option): Operation<Guid, ExistingCategory> =
    fun (categoryId: Guid) -> 
        match getCategory categoryId with
        | None -> Error (sprintf "Category with id '%O' does not exist" categoryId)
        | Some category -> Ok category 

let createWineCategory (getCategory: string -> ExistingCategory option) (addCategory: Guid * NewCategory -> _ option): Operation<NewCategory, Guid> = 
    fun (newCategory: NewCategory) -> 
        let create = 
            isValidCategoryInfo 
            >> Result.bind (isNotExistingCategory getCategory)
            >> Result.bind (fun category -> Ok (Guid.NewGuid(), category))
            >> Result.bind (fun (id, c) -> 
                match addCategory (id, c) with
                | Some _ -> Ok id
                | None -> Error (unableTo "add new category") )
        create newCategory

let deleteCategory (getCategory: Guid -> ExistingCategory option) (deleteCategory: Guid -> _ option): Operation<Guid, unit> =
    fun (categoryId: Guid) ->
        let delete = 
            Ok
            >> Result.bind (isExistingCategoryWithId getCategory)
            >> Result.bind (fun _ -> 
                match deleteCategory categoryId with
                | None -> Error  (unableTo "delete category")
                | Some _ -> Ok () )
        delete categoryId

            
let updateCategory (getCategory: GuidOrString -> ExistingCategory option) (updateCategory: EditCategory -> _ option ): Operation<Guid * EditCategory, unit> =
    
    let validCategoryUpdateInfo: Operation<EditCategory, EditCategory> =
        fun (editCategory: EditCategory) ->
            match editCategory with
            | {name=(Some n); description=(Some d)} -> validND (n,d) |> Result.map (fun _ -> editCategory)            
            | {name=(Some n)} -> validName n |> Result.map (fun _ -> editCategory)
            | {description=(Some d)} -> validDescription d |> Result.map (fun _ -> editCategory)
            | _ -> Error "Nothing to update"

    let notExistingCategoryName: Operation<Guid * EditCategory, EditCategory> =
        fun (id: Guid, editCategory: EditCategory) ->
            match editCategory with
            | {name=None} -> Ok editCategory
            | {name=Some editName} -> 
                match (String >> getCategory) editName with
                | Some c when c.id <> id -> Error (sprintf "A category with this name '%s' already exists" editName)
                | _ -> Ok editCategory

    fun (categoryId, editCategory) -> 
        let update =
            Ok
            >> Result.bind (isExistingCategoryWithId (Guid >> getCategory))
            >> Result.bind (fun _ -> validCategoryUpdateInfo editCategory )
            >> Result.bind (fun ec -> notExistingCategoryName (categoryId, ec))
            >> Result.bind (fun c ->
                match updateCategory c with
                | None -> Error (unableTo "update category")
                | Some _ -> Ok () )
        update categoryId   


/////////////////////////////////////////////////
//// Operations For Wine
//////////////////////////////////////////////////         

let private isValidYear = composeError (fun y -> y > 1240) "Year cannot be less than 1250"

let private isNotExistingWine (getCategory: string -> ExistingWine option): Operation<NewWine, NewWine> =
    fun (newWine: NewWine) -> 
        match getCategory newWine.name with
        | None -> Ok newWine
        | Some _ -> Error (sprintf "A wine with the name '%s' already Exists" newWine.name)

let private isExistingWineWithId (getWine: Guid -> ExistingWine option): Operation<Guid, ExistingWine> =
    fun (id: Guid) ->
        match getWine id with
        | Some wine -> Ok wine
        | None -> Error (sprintf "A wine with this %O does not exist" id)

let createWine getCategory getWine (addWine: (Guid * NewWine) -> _ option): Operation<NewWine, Guid> =
    fun (newWine: NewWine) ->
        let add = 
            Ok
            >> Result.bind (isNotExistingWine getWine)
            >> Result.bind (fun _ -> isExistingCategoryWithId getCategory newWine.categoryID)
            >> Result.bind (fun _ -> validND (newWine.name, newWine.description))
            >> Result.bind (fun _ ->  (Ok newWine.year))
            >> Result.bind (fun _ ->
                let id = Guid.NewGuid()
                match addWine (id, newWine) with
                | None -> Error (unableTo "add new wine")
                | Some _ -> Ok id )
        add newWine

let deleteWine getWine (deleteWine: Guid -> _ option) : Operation<Guid, unit> =
    fun (id: Guid) -> 
        let delete =
            Ok
            >> Result.bind (isExistingWineWithId getWine)
            >> Result.bind (fun _ -> 
                match deleteWine id with
                | None -> Error (unableTo "delete wine") 
                | Some _ -> Ok () )
        delete id

let updateWine getWine getCategory updateWine: Operation<Guid * EditWine, unit> =

    let validWineUpdateInfo: Operation<EditWine, EditWine> =
        fun (editWine: EditWine) ->
            match editWine with
            | {name=(Some n); description=(Some d); year=y} ->
                validND (n,d)
                |> Result.bind (fun _ -> y |> function None -> Ok 0 | Some year -> isValidYear (Ok year)) 
                |> Result.map (fun _ -> editWine)            
            | {name=(Some n); year=y} -> 
                validName n 
                |> Result.bind (fun _ -> y |> function None -> Ok 0| Some year -> isValidYear (Ok year))
                |> Result.map (fun _ -> editWine)
            | {description=(Some d); year=y} -> 
                validDescription d 
                |> Result.bind (fun _ -> y |> function None -> Ok 0 | Some year -> isValidYear (Ok year))
                |> Result.map (fun _ -> editWine)
            | _ -> Error "Nothing to update"

    let notExistingWineName: Operation<Guid * EditWine, EditWine> =
        fun (id: Guid, editWine: EditWine) ->
            match editWine with
            | {name=None} -> Ok editWine
            | {name=Some editName} -> 
                match (String >> getCategory) editName with
                | Some v when v.id <> id -> Error (sprintf "A category with this name '%s' already exists" editName)
                | _ -> Ok editWine

    fun (id, editWine) ->
        let update =
            Ok
            >> Result.bind validWineUpdateInfo
            >> Result.bind (fun ew -> notExistingWineName (id, ew))
            >> Result.bind (fun ew -> 
                match ew with
                | {categoryID=Some id} -> 
                    id
                    |> isExistingCategoryWithId (Guid >> getCategory) 
                    |> Result.bind (fun _ -> Ok ew)
                | _ -> Ok ew )
            >> Result.bind (fun ew -> 
                match updateWine (id,ew) with 
                | Some _ -> Ok ()
                | None -> Error (unableTo "update wine"))        
        update editWine
        
