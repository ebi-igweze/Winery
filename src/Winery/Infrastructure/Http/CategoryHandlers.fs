module Http.Categories

open Giraffe
open Microsoft.AspNetCore.Http
open Winery
open Storage.Models
open Http.Auth

let getCategories: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let queries = ctx.GetService<CategoryQueries>()
            let response = queries.getCategories()
            return! json response next ctx
        }

let getCategory id =
  fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let queries = ctx.GetService<CategoryQueries>()
            return! match queries.getCategoryById (CategoryID id) with
                    | Some c -> json c next ctx
                    | None -> notFound next ctx
        }

let deleteCategory id = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let queries = ctx.GetService<CategoryQueries>()
            let commands = ctx.GetService<CategoryCommands>()
            let removeCategory = removeCategoryWith queries.getCategoryById commands.deleteCategory 
            return! match (removeCategory <| (fakeAdmin, CategoryID id)) with
                    | Ok _ -> noContent next ctx
                    | Error e -> handleError e next ctx
        }

type EditInfo = {name: string; description: string}

let putCategory categoryId = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! editInfo = ctx.BindJsonAsync<EditInfo>()
            let queries = ctx.GetService<CategoryQueries>()
            let commands = ctx.GetService<CategoryCommands>()
            let editCategory = {EditCategory.name=hasValue editInfo.name id; description=hasValue editInfo.description id}
            let getCategoryByIdOrName = function
                | ID categoryId -> queries.getCategoryById categoryId
                | Name categoryName -> queries.getCategoryByName categoryName

            let updateCategory = editCategoryWith getCategoryByIdOrName commands.updateCategory
            return! match (updateCategory <| (fakeAdmin, CategoryID categoryId, editCategory)) with
                    | Ok _ -> noContent next ctx
                    | Error e -> handleError e next ctx
        }

let postCategory: HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! newCategory = ctx.BindJsonAsync<NewCategory>()
            let queries = ctx.GetService<CategoryQueries>()
            let commands = ctx.GetService<CategoryCommands>()
            return! match (addCategoryWith queries.getCategoryByName commands.addCategory <| (fakeAdmin, newCategory)) with
                    | Ok (CategoryID id) -> createdM (id.ToString("N")) next ctx
                    | Error e -> handleError e next ctx
        }

let categoryHttpHandlers: HttpHandler = 
        (choose [
            GET >=> choose [
                routeCi "/categories" >=> getCategories
                routeCif "/categories/%O"  getCategory
            ]
            PUT     >=> routeCif "/categories/%O" (authorizeAdminWithArgs << putCategory)
            POST    >=> routeCi "/categories" >=> authorizeAdmin >=> postCategory
            DELETE  >=> routeCif "/categories/%O" (authorizeAdminWithArgs << deleteCategory)
        ]) 
