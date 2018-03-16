[<AutoOpen>]
module Storage.Models

open Winery

type CategoryQueries = 
    { getCategories       : unit -> ExistingCategory list
      getCategoryById     : CategoryID -> ExistingCategory option
      getCategoryByName   : CategoryName -> ExistingCategory option }

type ExecutionResult = Result<string, string>

type CategoryCommandExecutioners =
    { addCategory      : CategoryID * NewCategory -> ExecutionResult
      updateCategory   : CategoryID * EditCategory -> ExecutionResult
      deleteCategory   : CategoryID -> ExecutionResult }

type WineQueries =
    { getWines                  : unit -> ExistingWine list
      getWineById               : WineID -> ExistingWine option
      getWineByName             : WineName -> ExistingWine option
      getWinesInCategory        : CategoryID -> ExistingWine list option
      getWineInCategoryById     : CategoryID -> WineID -> ExistingWine option
      getWineInCategoryByName   : CategoryID -> WineName -> ExistingWine option }

type WineCommandExecutioners = 
    { addWine      : CategoryID * WineID * NewWine -> ExecutionResult
      updateWine   : WineID * EditWine -> ExecutionResult
      deleteWine   : WineID -> ExecutionResult }

type CartQuery = UserID -> Cart option
type CartCommandExecutioner = CartAction -> ExecutionResult

type UserQueries = { getUser: IDorName<UserID, UserName> -> (ExistingUser * Password) option }
type UserCommandExecutioners = 
    { addUser: UserID * NewUser * Password -> ExecutionResult;
      updateUser: UserID * EditUser -> ExecutionResult  }