module Services.Actors.User

open Winery
open Akka.FSharp
open Storage.Models
open Services.Models
open System.Collections.Generic


let handleResult eventStream commandId = function 
    | Ok message -> publish (CommandCompleted (CommandID commandId, Message message, Success)) eventStream
    | Error message -> publish (CommandCompleted (CommandID commandId, Message message, Failure)) eventStream

type UserCommand =
| AddUser of UserID * NewUser * Password
| UpdateUser of UserID * EditUser
| AddUserRole of UserID * UserRole

let userActor (executioners: UserCommandExecutioners) (mailbox: Actor<_>)  =
    let eventStream = mailbox.Context.System.EventStream
    let rec loop () = actor {
        let! envelope = mailbox.Receive ()
        do publish (commandEnvelopeReveived envelope) eventStream
        let handleResult = handleResult eventStream envelope.id
        match envelope.message with
        | AddUser (userId, newUser, password) -> return! loop << handleResult <| executioners.addUser (userId, newUser, password)
        | UpdateUser (userId, editUser)       -> return! loop << handleResult <| executioners.updateUser (userId, editUser)
        | AddUserRole _                       -> return! loop ()
    }
    loop ()

let cartActor (executioner: CartCommandExecutioner) (mailbox: Actor<_>) =
    let eventStream = mailbox.Context.System.EventStream
    let rec loop () = actor {
        let! envelope = mailbox.Receive ()
        do publish (commandEnvelopeReveived envelope) eventStream 
        let handleResult = handleResult eventStream envelope.id
        return!  loop << handleResult <| (executioner envelope.message)
    }
    loop ()

let commandActor (mailbox: Actor<_>) = 
    let channel = typeof<CommandAction>
    let eventStream = mailbox.Context.System.EventStream
    
    // subscribe to command actions
    do subscribe channel mailbox.Self eventStream |> ignore

    let tryGetCommand commandId = 
        Seq.tryFind (fun (v : KeyValuePair<CommandID,_>) -> v.Key = commandId) 
        >> Option.map (fun keyValue -> keyValue.Value)


    let rec loop (store: Dictionary<CommandID, Command>) = actor {
        let! message = mailbox.Receive ()
        return! match message with 
                | CommandReceived command -> 
                    store.Add <| (command.id, command)
                    loop store 

                | CommandCompleted (commandId, Message message, status) -> 
                    store
                    |> tryGetCommand commandId
                    |> function
                        | None         -> ()
                        | Some command -> 
                            command.resultStatus <- status
                            command.resultMessage <- message
                            store.[commandId] <- command
                    |> ignore
                    loop store

                | CommandStatusRequest commandId -> 
                    let command = tryGetCommand commandId <| store
                    let commandResult = command |> Option.map (fun command -> 
                        let (CommandID id) = command.id
                        { CommandResult.id = id
                          message = command.resultMessage; 
                          result = command.resultStatus; })

                    do mailbox.Sender() <! commandResult
                    loop store
    }

    loop (Dictionary())

let getUserReceivers userActor: UserCommandReceivers = {
    addUser = fun args ->
        let command = envelopeWithDefaults (AddUser args)
        userActor <! command
        CommandID command.id
    updateUser = fun args ->
        let command = envelopeWithDefaults (UpdateUser args)
        userActor <! command
        CommandID command.id }

let getCartReceiver cartActor: CartCommandReceiver = fun args -> 
    let command = envelopeWithDefaults args
    cartActor <! command
    CommandID command.id

let getCommandAgent commandActor: CommandAgent = 
    let checkStatus = fun commandId ->
        async {
            let! result = commandActor <? (CommandStatusRequest commandId)
            return result
        } |> Async.StartAsTask
    { checkStatus = checkStatus }