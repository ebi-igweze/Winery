module Services.Actors.Storage

open Akka.FSharp
open Storage
open Services.Models

let categoryActor (executioners: CategoryCommandExecutioners) (mailbox: Actor<_>) = 
    let rec handleResult = function 
        | Some _ -> loop () // handle success
        | None   -> loop () // handle error

    and loop () = actor {
        let! message = mailbox.Receive ()
        match message with
        | DeleteCategory (categoryId)               -> return! handleResult <| executioners.deleteCategory categoryId
        | AddCategory (categoryId, newCategory)     -> return! handleResult <| executioners.addCategory (categoryId, newCategory)            
        | UpdateCategory (categoryId, editCategory) -> return! handleResult <| executioners.updateCategory (categoryId, editCategory)  
    }
    loop ()   

let wineActor (executioners: WineCommandExecutioners) (mailbox: Actor<_>) =
    let rec handleResult = function
        | Some _ -> loop () // handle success
        | None   -> loop () // handle error

    and loop () = actor {
        let! message = mailbox.Receive ()
        match message with 
        | DeleteWine wineId                     -> return! handleResult <| executioners.deleteWine wineId
        | UpdateWine (wineId, editWine)         -> return! handleResult <| executioners.updateWine (wineId, editWine)
        | AddWine (categoryId, wineId, newWine) -> return! handleResult <| executioners.addWine (categoryId, wineId, newWine)
    }
    loop ()

let getWineReceivers wineActor: WineCommandReceivers =
    { addWine = fun args ->  wineActor <! (AddWine args)
      deleteWine = fun args -> wineActor <! (DeleteWine args)
      updateWine = fun args -> wineActor <! (UpdateWine args) }

let getCategoryReceivers categoryActor: CategoryCommandReceivers =
    { addCategory = fun args -> categoryActor <! (AddCategory args) 
      deleteCategory = fun args -> categoryActor <! (DeleteCategory args) 
      updateCategory = fun args -> categoryActor <! (UpdateCategory args) }

