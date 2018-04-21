module Http.Users

open Giraffe
open Microsoft.AspNetCore.Http
open Services.Models
open Storage.Models
open Winery

type Notification<'T> = { status: string; result: 'T }

type EditInfo = 
    { email: string
      firstName: string
      lastName: string }

let putUserInfo userId: HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let isValid (s: string) = not (isNull s && s = "" && s = " ")
            let getValue s = if isValid s then Some s else None
            let userQuery = ctx.GetService<UserQueries>()
            let! editInfo = ctx.BindJsonAsync<EditInfo>()
            let editUserInfo = 
                { EditUser.email = getValue editInfo.email; 
                  firstName = getValue editInfo.firstName; 
                  lastName = getValue editInfo.lastName }
            let receivers = ctx.GetService<UserCommandReceivers>()
            let getUser = userQuery.getUser >> function | Some (user, _) -> Some user | _ -> None
            let updateUser = editUser getUser receivers.updateUser  
            return! (handleCommand next ctx <<  updateUser <| (UserID userId, editUserInfo))
        }

let userHttpHandlers: HttpHandler = 
    (choose [
        routeCif "/users/%O" putUserInfo
    ])