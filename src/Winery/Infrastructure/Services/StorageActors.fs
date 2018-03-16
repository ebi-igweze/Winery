module Services.Actors.Storage

open Akka.FSharp
open Storage
open Services.Models
open Services.Actors.User

let categoryActor (executioners: CategoryCommandExecutioners) (mailbox: Actor<_>) = 
    let eventStream = mailbox.Context.System.EventStream
    let rec loop () = actor {
        let! envelope = mailbox.Receive ()
        do publish (commandEnvelopeReveived envelope) eventStream
        let handleResult =  handleResult eventStream envelope.id
        match envelope.message with
        | UpdateCategory (categoryId, editCategory) -> return! loop << handleResult <| executioners.updateCategory (categoryId, editCategory)  
        | AddCategory (categoryId, newCategory)     -> return! loop << handleResult <| executioners.addCategory (categoryId, newCategory)            
        | DeleteCategory (categoryId)               -> return! loop << handleResult <| executioners.deleteCategory categoryId
    }
    loop ()   

let wineActor (executioners: WineCommandExecutioners) (mailbox: Actor<_>) =
    let eventStream = mailbox.Context.System.EventStream
    let rec loop () = actor {
        let! envelope = mailbox.Receive ()
        do publish (commandEnvelopeReveived envelope) eventStream
        let handleResult = handleResult eventStream envelope.id
        match envelope.message with 
        | DeleteWine wineId                     -> return! loop << handleResult <| executioners.deleteWine wineId
        | UpdateWine (wineId, editWine)         -> return! loop << handleResult <| executioners.updateWine (wineId, editWine)
        | AddWine (categoryId, wineId, newWine) -> return! loop << handleResult <| executioners.addWine (categoryId, wineId, newWine)
    }
    loop ()

let getWineReceivers wineActor: WineCommandReceivers = {
    addWine = fun args ->  
        let command = envelopeWithDefaults (AddWine args)
        wineActor <! command
        CommandID command.id
    deleteWine = fun args ->
        let command = envelopeWithDefaults (DeleteWine args)
        wineActor <! command
        CommandID command.id
    updateWine = fun args ->
        let command = envelopeWithDefaults (UpdateWine args)
        wineActor <! command
        CommandID command.id }

let getCategoryReceivers categoryActor: CategoryCommandReceivers = {
    addCategory = fun args ->
        let command = envelopeWithDefaults (AddCategory args)  
        categoryActor <! command
        CommandID command.id
    deleteCategory = fun args ->
        let command = envelopeWithDefaults (DeleteCategory args) 
        categoryActor <! command
        CommandID command.id
    updateCategory = fun args ->
        let command = envelopeWithDefaults (UpdateCategory args)
        categoryActor <! command
        CommandID command.id    }

