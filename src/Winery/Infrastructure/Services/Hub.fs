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
        Task.Run(fun () -> printfn "user with id connected %s" this.Context.ConnectionId)
        
    override this.OnDisconnectedAsync _ = 
        Task.Run(fun () -> printfn "%s left" this.Context.ConnectionId)


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

