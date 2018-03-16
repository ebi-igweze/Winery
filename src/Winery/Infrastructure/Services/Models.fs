module Services.Models

open Winery
open System
open System.Threading.Tasks


type ResultStatus = 
    | NotCompleted
    | Success 
    | Failure with
    override this.ToString () = this |> function | Success -> "SUCCESS" | Failure -> "FAILURE" | NotCompleted -> "NOTCOMPLETED"

type CommandID = CommandID of Guid
type CommandMessage = Message of string

type Command = 
    { id: CommandID;
      time: DateTime;
      mutable resultMessage: string;
      mutable resultStatus: ResultStatus; }

type Envelope<'T> = 
    { id: Guid;
      message: 'T;
      time: DateTime; }

type CommandResult = 
    { id: Guid
      message: string
      result: ResultStatus }   

type CommandAction = 
    | CommandReceived of Command
    | CommandCompleted of CommandID * CommandMessage * ResultStatus
    | CommandStatusRequest of CommandID
    
type CommandAgent = { checkStatus : CommandID -> Task<CommandResult option> }

type AuthService =
    { hashPassword: string -> string 
      verify: string * string -> bool }

type CategoryCommand = 
    | AddCategory    of categoryId : CategoryID * newCategory : NewCategory 
    | UpdateCategory of categoryId : CategoryID * editCategory : EditCategory 
    | DeleteCategory of categoryId : CategoryID

type WineCommand =
    | AddWine    of categoryId : CategoryID * wineId : WineID * newWine : NewWine
    | UpdateWine of wineId : WineID * editWine : EditWine
    | DeleteWine of wineId : WineID

type CategoryCommandReceivers = 
    { addCategory      : CategoryID * NewCategory -> CommandID 
      updateCategory   : CategoryID * EditCategory -> CommandID
      deleteCategory   : CategoryID -> CommandID }

type WineCommandReceivers = 
    { addWine      : CategoryID * WineID * NewWine -> CommandID
      updateWine   : WineID * EditWine -> CommandID 
      deleteWine   : WineID -> CommandID }

type UserCommandReceivers = 
    { addUser    : UserID * NewUser * Password -> CommandID
      updateUser : UserID * EditUser -> CommandID }

type CartCommandReceiver = CartAction -> CommandID


let envelopeWithDefaults message = 
    { id = Guid.NewGuid();
      message = message; 
      time = DateTime.UtcNow; } 

let commandDefaultFromEnvelope (envelope: Envelope<_>) =
    { id = CommandID envelope.id;
      time = envelope.time
      resultMessage = "";
      resultStatus = NotCompleted }

let commandEnvelopeReveived m = CommandReceived << commandDefaultFromEnvelope <| m
