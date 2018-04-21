module Services.Actors.User

open System
open Winery
open Akka.FSharp
open Storage.Models
open Services.Models
open Winery.Services.Hubs
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.DependencyInjection

let mutable sp: IServiceProvider = null

let handleResult sender commandId = function 
    | Ok message    -> sender <! (CommandCompleted (CommandID commandId, Message message, Success))
    | Error message -> sender <! (CommandCompleted (CommandID commandId, Message message, Failure))

let userActor (executioners: UserCommandExecutioners) (mailbox: Actor<_>)  =
    let rec loop () = actor {
        let! envelope = mailbox.Receive ()
        let handleResult = handleResult (mailbox.Sender()) envelope.id
        match envelope.message with
        | AddUserRole _                       -> return! loop ()
        | UpdateUser (userId, editUser)       -> return! loop << handleResult <| executioners.updateUser (userId, editUser)
        | AddUser (userId, newUser, password) -> return! loop << handleResult <| executioners.addUser (userId, newUser, password)
    }
    loop ()

let cartActor (executioner: CartCommandExecutioner) (mailbox: Actor<_>) =
    let rec loop () = actor {
        let! envelope = mailbox.Receive ()
        let handleResult = handleResult (mailbox.Sender()) envelope.id
        return! loop << handleResult <| (executioner envelope.message)
    }
    loop ()

let commandActor userActorRef cartActorRef wineActorRef categoryActorRef (mailbox: Actor<_>) = 

    let handleCommand envelope = envelope.message |> function 
        | WineCommand command     -> wineActorRef     <! copyEnvelope command envelope 
        | UserCommand command     -> userActorRef     <! copyEnvelope command envelope
        | CartCommand command     -> cartActorRef     <! copyEnvelope command envelope
        | CategoryCommand command -> categoryActorRef <! copyEnvelope command envelope

    let filter (commandResult: CommandResult) = fun (commandId, _) -> commandId = (CommandID commandResult.id)

    let handleCompletion commandResult store = 
        let withCommandId = filter commandResult
        store
        |> Seq.tryFind withCommandId
        |> function
            | None               -> async { return store }
            | Some (_, envelope) -> async {
                    let wineHub = sp.GetService<IHubContext<WineHub, IWineClient>>()
                    let commandStr = (string envelope.message)
                    do! wineHub.Clients.All.CommandCompleted(commandStr, commandResult) |> Async.AwaitTask
                    return store |> List.filter (not << withCommandId) } 
        |> Async.RunSynchronously

    let rec loop (store: list<CommandID * Envelope<_>>) = actor {
        let! message = mailbox.Receive ()
        let newstore =
            match message with 
            | CommandReceived command ->
                handleCommand command
                ((CommandID command.id, command)::store)

            | CommandCompleted (CommandID id, Message message, status) -> 
                let commandResult = { id = id; message = message; result = status}
                handleCompletion commandResult store
        return! loop newstore
    }

    loop []

let getUserReceivers commandActor: UserCommandReceivers = {
    addUser = fun args ->
        let command = envelopeWithDefaults (UserCommand (AddUser args))
        commandActor <! CommandReceived command
        CommandID command.id
    updateUser = fun args ->
        let command = envelopeWithDefaults (UserCommand (UpdateUser args))
        commandActor <! CommandReceived command
        CommandID command.id }

let getCartReceiver commandActor: CartCommandReceiver = fun args -> 
    let command = envelopeWithDefaults (CartCommand args)
    commandActor <! CommandReceived command
    CommandID command.id
