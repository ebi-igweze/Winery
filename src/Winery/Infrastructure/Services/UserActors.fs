module Services.Actors.User

open Winery
open System
open Akka.FSharp
open Storage.Models
open Services.Models
open System.Collections.Generic

let publishEvent (mailbox: Actor<_>) event = do mailbox.Context.System.EventStream.Publish event
let subscribeEvent (mailbox: Actor<_>) event = mailbox.Context.System.EventStream.Subscribe event


type UserCommand =
| AddUser of UserID * NewUser * Password
| UpdateUser of UserID * EditUser
| AddUserRole of UserID * UserRole

let userActor (executioners: UserCommandExecutioners) (mailbox: Actor<_>)  =
    let rec handleResult (envelope: Envelope<_>) = function
        | Ok message -> 
            do (CommandCompleted (CommandID envelope.id, Message message, Success))
               |> publishEvent mailbox
            loop ()

        | Error message ->
            do (CommandCompleted (CommandID envelope.id, Message message, Failure))
               |> publishEvent mailbox
            loop ()

    and loop () = actor {
        let! envelope = mailbox.Receive ()
        do publishEvent mailbox (commandEnvelopeReveived envelope)
        match envelope.message with
        | AddUser (userId, newUser, password) -> return! handleResult envelope <| executioners.addUser (userId, newUser, password)
        | UpdateUser (userId, editUser) -> return! handleResult envelope <| executioners.updateUser (userId, editUser)
        | AddUserRole _ -> return! loop ()
    }

    loop ()

let cartActor (executioner: CartCommandExecutioner) (mailbox: Actor<_>) =
    let rec handleResult (envelope: Envelope<_>) = function
        | Ok message -> 
            do (CommandCompleted (CommandID envelope.id, Message message, Success))
               |> publishEvent mailbox
            loop ()
        | Error message ->
            do (CommandCompleted (CommandID envelope.id, Message message, Failure))
               |> publishEvent mailbox
            loop ()

    and loop () = actor {
        let! envelope = mailbox.Receive ()
        do publishEvent mailbox <| (commandEnvelopeReveived envelope)
        return! handleResult envelope <| (executioner envelope.message)
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

    let rec handleResult (store: IDictionary<CommandID, Command>) = function 
        | CommandReceived command -> 
            printfn ("received command")
            do KeyValuePair(command.id, (command)) |> store.Add
            loop store

        | CommandCompleted (commandId, Message message, status) -> 
            printfn ("command completed")
            store
            |> tryGetCommand commandId
            |> function
                | Some command -> 
                    command.resultStatus <- status
                    command.resultMessage <- message
                    store.[commandId] <- command
                | None -> ()
            |> ignore
            loop store

        | CommandStatusRequest commandId -> 
            printfn ("command requested")
            let command = tryGetCommand commandId <| store
            let commandResult = 
                command 
                |> Option.map (fun command -> 
                      let (CommandID id) = command.id
                      { CommandResult.id = id
                        message = command.resultMessage; 
                        result = command.resultStatus; })

            do mailbox.Sender() <! commandResult
            loop store

    and loop store = actor {
        let! message = mailbox.Receive ()
        return! handleResult store <| message 
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