module Http.Categories

open Giraffe
open Microsoft.AspNetCore.Http
open Winery
open Storage.InMemory

let getCategories: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let store = ctx.GetService<Storage>()
            let response = store.getCategories()
            return! json response next ctx
        }

let getCategory id =
  fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let store = ctx.GetService<Storage>()
            return! match store.getCategoryById (CategoryID id) with
                    | Some c -> json c next ctx
                    | None -> notFound next ctx
        }

let deleteCategory id = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let store = ctx.GetService<Storage>()
            return! match (removeCategoryWith store.getCategoryById store.deleteCategory <| (fakeAdmin, CategoryID id)) with
                    | Ok _ -> noContent next ctx
                    | Error e -> handleError e next ctx
        }

type EditInfo = {name: string; description: string}

let putCategory categoryId = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! editInfo = ctx.BindJsonAsync<EditInfo>()
            let store = ctx.GetService<Storage>()
            let editCategory = {EditCategory.name=hasValue editInfo.name id; description=hasValue editInfo.description id}
            let getCategoryByIdOrName = function
                | ID categoryId -> store.getCategoryById categoryId
                | Name categoryName -> store.getCategoryByName categoryName

            let updateCategory = editCategoryWith getCategoryByIdOrName store.updateCategory
            return! match (updateCategory <| (fakeAdmin, CategoryID categoryId, editCategory)) with
                    | Ok _ -> noContent next ctx
                    | Error e -> handleError e next ctx
        }

let postCategory: HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! newCategory = ctx.BindJsonAsync<NewCategory>()
            let store = ctx.GetService<Storage>()
            return! match (addCategoryWith store.getCategoryByName store.addCategory <| (fakeAdmin, newCategory)) with
                    | Ok (CategoryID id) -> createdM (id.ToString("N")) next ctx
                    | Error e -> handleError e next ctx
        }

let categoryHttpHandlers: HttpHandler = 
        (choose [
            GET >=> choose [
                routeCi "/categories" >=> getCategories
                routeCif "/categories/%O"  getCategory
            ]
            PUT     >=> routeCif "/categories/%O" putCategory
            POST    >=> routeCi "/categories" >=> postCategory
            DELETE  >=> routeCif "/categories/%O" deleteCategory
        ]) 
