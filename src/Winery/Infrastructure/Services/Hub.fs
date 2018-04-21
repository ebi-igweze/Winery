module Winery.Services.Hubs

open Microsoft.AspNetCore.SignalR
open System.Threading.Tasks
open Services.Models

type IWineClient = 
    abstract member CommandCompleted: string * CommandResult -> Task
    abstract member Send: obj -> Task

type WineHub() =
    inherit Hub<IWineClient>()
    override  this.OnConnectedAsync() = 
        do printfn "user with id connected %s" this.Context.ConnectionId
        let message = sprintf "%s left" this.Context.ConnectionId
        this.Clients.All.CommandCompleted(message, {id=System.Guid(); message=message; result=Failure})
        
    override this.OnDisconnectedAsync _ = 
        let message = sprintf "%s left" this.Context.ConnectionId
        this.Clients.All.Send(message)

    member this.Send (message: string) =
        do printfn "user with connectionId: %s disconnected" this.Context.ConnectionId
        let message' = sprintf "%s %s" this.Context.ConnectionId message
        this.Clients.All.Send(message')


type ClientCommands =
| AddWine
| UpdateWine
| DeleteWine
| AddCategory
| UpdateCategory
| DeleteCategory
| UpdateWineInventory
| AddItemToCart
| RemoveItemFromCart
| PlaceOrder

