module Services.Actors.Storage

open Akka.FSharp
open Storage
open Services.Models
open Services.Actors.User

let categoryActor (executioners: CategoryCommandExecutioners) (mailbox: Actor<_>) = 
    let rec loop () = actor {
        let! envelope = mailbox.Receive ()
        let handleResult = handleResult (mailbox.Sender()) envelope.id
        match envelope.message with
        | UpdateCategory (categoryId, editCategory) -> return! loop << handleResult <| executioners.updateCategory (categoryId, editCategory)  
        | AddCategory (categoryId, newCategory)     -> return! loop << handleResult <| executioners.addCategory (categoryId, newCategory)            
        | DeleteCategory (categoryId)               -> return! loop << handleResult <| executioners.deleteCategory categoryId
    }
    loop ()   

let wineActor (executioners: WineCommandExecutioners) (mailbox: Actor<_>) =
    let rec loop () = actor {
        let! envelope = mailbox.Receive ()
        let handleResult = handleResult (mailbox.Sender()) envelope.id
        match envelope.message with 
        | DeleteWine wineId                     -> return! loop << handleResult <| executioners.deleteWine wineId
        | UpdateWine (wineId, editWine)         -> return! loop << handleResult <| executioners.updateWine (wineId, editWine)
        | AddWine (categoryId, wineId, newWine) -> return! loop << handleResult <| executioners.addWine (categoryId, wineId, newWine)
    }
    loop ()

let getWineReceivers commandActor: WineCommandReceivers = {
    addWine = fun args ->  
        let command = envelopeWithDefaults (WineCommand (AddWine args))
        commandActor <! CommandReceived command
        CommandID command.id
    deleteWine = fun args ->
        let command = envelopeWithDefaults (WineCommand (DeleteWine args))
        commandActor <! CommandReceived command
        CommandID command.id
    updateWine = fun args ->
        let command = envelopeWithDefaults (WineCommand (UpdateWine args))
        commandActor <! CommandReceived command
        CommandID command.id }

let getCategoryReceivers commandActor: CategoryCommandReceivers = {
    addCategory = fun args ->
        let command = envelopeWithDefaults (CategoryCommand (AddCategory args))
        commandActor <! CommandReceived command
        CommandID command.id
    deleteCategory = fun args ->
        let command = envelopeWithDefaults (CategoryCommand (DeleteCategory args))
        commandActor <! CommandReceived command
        CommandID command.id
    updateCategory = fun args ->
        let command = envelopeWithDefaults (CategoryCommand (UpdateCategory args))
        commandActor <! CommandReceived command
        CommandID command.id    }

