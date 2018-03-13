[<AutoOpen>]
module Storage.Models

open Winery

type CategoryQueries = 
    { getCategories       : unit -> ExistingCategory list
      getCategoryById     : CategoryID -> ExistingCategory option
      getCategoryByName   : CategoryName -> ExistingCategory option }

type CategoryCommandExecutioners =
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

type WineCommandExecutioners = 
    { addWine      : CategoryID * WineID * NewWine -> unit option
      updateWine   : WineID * EditWine -> unit option
      deleteWine   : WineID -> unit option }

type CartQuery = UserID -> Cart option
type CartCommandExecutioner = CartAction -> unit option
type UserQueries = { getUser: UserName -> (ExistingUser * Password) option }
type UserCommandExecutioners = { addUser: UserID * NewUser * Password -> unit option }