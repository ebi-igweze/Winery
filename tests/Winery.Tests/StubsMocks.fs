[<AutoOpen>]
module Tests.StubsMocks

open Winery
open System


let utilityID = Guid("8c687b7fae2e458082b5c29c7c6e0fd6")
let userID = Guid("4ec87f064d1e41b49342ab1aead1f99d")
let cartItemID = Guid("9efe6f5c6a1f4437a8ba62abac745ce8")
let fakeID = Guid("00000000-0000-0000-0000-000000000000")

let wine = {ExistingWine.id=utilityID;name="wine name";description="some desc";year=1608;price=14m;imagePath="img/path";categoryID=utilityID}

let emptyCart = {Cart.userId=userID; items=[||]}
let cartWithItem = {Cart.userId=userID; items=[|{id=cartItemID;product=wine;quantity=5us}|]}


let getNoCart _ = None
let getEmptyCart _ = Some emptyCart
let getCartWithItem _ = Some cartWithItem

let existingCategory = {ExistingCategory.id=fakeID; name="Sparkling Wine"; description="A class of table Wines"; wines=[]}
let getSomeCategory _ = Some existingCategory
let getNoCategory _ = None

let getSomeWine _ = Some wine
let getNoWine _ = None