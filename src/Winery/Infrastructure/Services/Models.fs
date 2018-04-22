module Services.Models

open Winery
open System

type ResultStatus = 
    | Success 
    | Failure with
    override this.ToString () = this |> function | Success -> "SUCCESS" | Failure -> "FAILURE"

type CommandID = CommandID of Guid
type CommandMessage = Message of string

type AuthService =
    { hashPassword: string -> string 
      verify: string * string -> bool }

type CategoryCommand = 
    | AddCategory    of CategoryID * NewCategory 
    | UpdateCategory of CategoryID * EditCategory 
    | DeleteCategory of CategoryID
    with override this.ToString() = this |> function AddCategory _ -> "AddCategory" | UpdateCategory _ -> "UpdateCategory" | DeleteCategory _ -> "DeleteCategory"

type WineCommand =
    | AddWine    of CategoryID * WineID * NewWine
    | UpdateWine of WineID * EditWine
    | DeleteWine of WineID
    with override this.ToString() = this |> function AddWine _ -> "AddWine" | UpdateWine _ -> "UpdateWine" | DeleteWine _ -> "DeleteWine"

type UserCommand =
    | AddUser     of UserID * NewUser * Password
    | UpdateUser  of UserID * EditUser
    | AddUserRole of UserID * UserRole 
    with override this.ToString() = this |> function AddUser _ -> "AddUser" | UpdateUser _ -> "UpdateUser" | AddUserRole _ -> "AddUserRole"

type SystemCommand =
    | WineCommand     of WineCommand
    | CartCommand     of CartAction
    | UserCommand     of UserCommand
    | CategoryCommand of CategoryCommand
    with override this.ToString() = this |> function WineCommand c -> string c | CartCommand c -> string c | UserCommand c -> string c | CategoryCommand c -> string c

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

type CommandResult = 
    { id: Guid
      message: string
      result: ResultStatus }   

type Envelope<'T> = 
    { id: Guid;
      time: DateTime; 
      message: 'T }

type CommandAction = 
    | CommandReceived of Envelope<SystemCommand>
    | CommandCompleted of CommandID * CommandMessage * ResultStatus    

let envelopeWithId id message =
    { id = id
      message = message
      time = DateTime.UtcNow }
      
let copyEnvelope message envelope =
    { id = envelope.id
      message = message
      time = envelope.time }