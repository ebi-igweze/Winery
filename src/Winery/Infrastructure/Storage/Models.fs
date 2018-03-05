[<AutoOpen>]
module Storage.Models

open Winery

type CategoryQueries = 
    { getCategories       : unit -> ExistingCategory list
      getCategoryById     : CategoryID -> ExistingCategory option
      getCategoryByName   : CategoryName -> ExistingCategory option }

type CategoryCommands =
    { addCategory      : CategoryID * NewCategory -> unit option
      updateCategory   : CategoryID * EditCategory -> unit option
      deleteCategory   : CategoryID -> unit option }

type WineQueries =
    { getWines                  : unit -> ExistingWine list
      getWineById               : WineID -> ExistingWine option
      getWineByName             : WineName -> ExistingWine option
      getWinesInCategory        : CategoryID -> ExistingWine list option
      getWineInCategoryById     : CategoryID -> WineID -> ExistingWine option
      getWineInCategoryByName   : CategoryID -> WineName -> ExistingWine option }

type WineCommands = 
    { addWine      : CategoryID * WineID * NewWine -> unit option
      updateWine   : WineID * EditWine -> unit option
      deleteWine   : WineID -> unit option }

type CartQuery = UserID -> Cart option
type CartCommand = CartAction -> unit option
type UserQueries = { getUser: UserName -> (ExistingUser * Password) option }
type UserCommands = { addUser: UserID * NewUser * Password -> unit option }