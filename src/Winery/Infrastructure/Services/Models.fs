module Services.Models

open Winery

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
    { addCategory      : CategoryID * NewCategory -> unit 
      updateCategory   : CategoryID * EditCategory -> unit
      deleteCategory   : CategoryID -> unit }

type WineCommandReceivers = 
    { addWine      : CategoryID * WineID * NewWine -> unit
      updateWine   : WineID * EditWine -> unit 
      deleteWine   : WineID -> unit }

type UserCommandReceivers = 
    { addUser: UserID * NewUser * Password -> unit }

type CartCommandReceiver = CartAction -> unit