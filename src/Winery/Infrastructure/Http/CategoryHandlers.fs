module Http.Categories

open Giraffe
open Microsoft.AspNetCore.Http
open Winery
open Storage.Models
open Http.Auth
open Services.Models

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

let getCategoryWithName =
  fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let nameOption = ctx.TryGetQueryStringValue("name")
            return! match nameOption with 
                    | None -> notFound next ctx
                    | Some name ->
                        let queries = ctx.GetService<CategoryQueries>()
                        match queries.getCategoryByName (CategoryName name) with
                        | Some c -> json c next ctx
                        | None -> notFound next ctx
        }

let deleteCategory id = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let queries = ctx.GetService<CategoryQueries>()
            let commandReceivers = ctx.GetService<CategoryCommandReceivers>()
            let removeCategory = removeCategory queries.getCategoryById commandReceivers.deleteCategory 
            return! (handleCommand next ctx << removeCategory <| (fakeAdmin, CategoryID id))
        }

type EditInfo = { name : string; description : string }

let putCategory categoryId = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! editInfo = ctx.BindJsonAsync<EditInfo>()
            let queries = ctx.GetService<CategoryQueries>()
            let commandReceivers = ctx.GetService<CategoryCommandReceivers>()
            let editCategoryInfo = {EditCategory.name=hasValue editInfo.name id; description=hasValue editInfo.description id}
            let getCategoryByIdOrName = function
                | ID categoryId -> queries.getCategoryById categoryId
                | Name categoryName -> queries.getCategoryByName categoryName

            let updateCategory = editCategory getCategoryByIdOrName commandReceivers.updateCategory
            return! (handleCommand next ctx << updateCategory <| (fakeAdmin, CategoryID categoryId, editCategoryInfo)) 
        }

let postCategory: HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! newCategory = ctx.BindJsonAsync<NewCategory>()
            let queries = ctx.GetService<CategoryQueries>()
            let commandReceivers = ctx.GetService<CategoryCommandReceivers>()
            return! (handleCommand next ctx << addCategory queries.getCategoryByName commandReceivers.addCategory <| (fakeAdmin, newCategory))
        }

let categoryHttpHandlers: HttpHandler = 
        (choose [
            GET >=> choose [
                routeCi "/categories" >=> getCategories
                routeCi "/categories/search" >=> getCategoryWithName
                routeCif "/categories/%O" getCategory
            ]
            PUT     >=> routeCif "/categories/%O" (authorizeAdminWithArgs << putCategory)
            POST    >=> routeCi "/categories" >=> authorizeAdmin >=> postCategory
            DELETE  >=> routeCif "/categories/%O" (authorizeAdminWithArgs << deleteCategory)
        ]) 
