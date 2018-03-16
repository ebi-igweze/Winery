module Services.Actors.Storage

open Akka.FSharp
open Storage
open Services.Models
open Services.Actors.User

let categoryActor (executioners: CategoryCommandExecutioners) (mailbox: Actor<_>) = 
    let rec handleResult (envelope: Envelope<_>) = function 
        | Ok message ->
            do (CommandCompleted (CommandID envelope.id, Message message, Success))
               |> publishEvent mailbox
            loop ()
        | Error message -> 
            do (CommandCompleted (CommandID envelope.id, Message message, Success))
               |> publishEvent mailbox
            loop ()

    and loop () = actor {
        let! envelope = mailbox.Receive ()
        printfn "category actor received %O" envelope
        do publish (commandEnvelopeReveived envelope) mailbox.Context.System.EventStream
        match envelope.message with
        | DeleteCategory (categoryId)               -> return! handleResult envelope <| executioners.deleteCategory categoryId
        | AddCategory (categoryId, newCategory)     -> return! handleResult envelope <| executioners.addCategory (categoryId, newCategory)            
        | UpdateCategory (categoryId, editCategory) -> return! handleResult envelope <| executioners.updateCategory (categoryId, editCategory)  
    }
    loop ()   

let wineActor (executioners: WineCommandExecutioners) (mailbox: Actor<_>) =
    let rec handleResult (envelope: Envelope<_>) = function
        | Ok message ->
            do (CommandCompleted (CommandID envelope.id, Message message, Success))
               |> publishEvent mailbox
            loop ()
        | Error message -> 
            do (CommandCompleted (CommandID envelope.id, Message message, Success))
               |> publishEvent mailbox
            loop ()

    and loop () = actor {
        let! envelope = mailbox.Receive ()

        do publishEvent mailbox <| (commandEnvelopeReveived envelope)
        match envelope.message with 
        | DeleteWine wineId                     -> return! handleResult envelope <| executioners.deleteWine wineId
        | UpdateWine (wineId, editWine)         -> return! handleResult envelope <| executioners.updateWine (wineId, editWine)
        | AddWine (categoryId, wineId, newWine) -> return! handleResult envelope <| executioners.addWine (categoryId, wineId, newWine)
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

