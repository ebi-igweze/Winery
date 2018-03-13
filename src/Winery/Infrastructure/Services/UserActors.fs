module Services.Actors.User

open Akka.FSharp
open Storage.Models
open Winery
open Services.Models

type UserCommand =
| AddUser of UserID * NewUser * Password
| AddUserRole of UserID * UserRole

let userActor (executioners: UserCommandExecutioners) (mailbox: Actor<_>)  =
    let rec handleResult = function
        | Some _ -> loop ()
        | None -> loop ()

    and loop () = actor {
        let! message = mailbox.Receive ()
        match message with
        | AddUser (userId, newUser, password) -> return! handleResult <| executioners.addUser (userId, newUser, password)
        | AddUserRole _ -> return! loop ()
    }

    loop ()

let cartActor (executioner: CartCommandExecutioner) (mailbox: Actor<_>) =
    let rec handleResult = function
        | Some _ -> loop ()
        | None   -> loop ()

    and loop () = actor {
        let! message = mailbox.Receive ()
        return! handleResult <| (executioner message)
    }

    loop ()


let getUserReceivers userActor: UserCommandReceivers = { addUser = fun args -> userActor <! (AddUser args) }

let cartReceiver cartActor: CartCommandReceiver = fun args -> cartActor <! args