namespace DataStore.Migrations

open Winery
open FluentMigrator
open System
open Storage.InMemory

type Category = {
    id: Guid
    name: string 
    description: string }

[<Migration(201803230723L)>]
type ``Initial DB Creation``() =
    inherit Migration()

    override this.Up () =
        this.CreateCategoryTable()
        this.CreateWineTable()
        this.CreateWineInventory()
        this.CreateUserTable()    
        this.CreateCartAndOrderTable()    
        this.SeedDatabase()

    override this.Down () =
        this.Delete.Table("WineInventory") |> ignore
        this.Delete.Table("CartItem") |> ignore
        this.Delete.Table("Cart") |> ignore
        this.Delete.Table("OrderItem") |> ignore
        this.Delete.Table("Order") |> ignore
        this.Delete.Table("Wine") |> ignore
        this.Delete.Table("Category") |> ignore
        this.Delete.Table("User") |> ignore


    member private this.CreateCategoryTable() =
        this.Create.Table("Category")
            .WithColumn("ID").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("Name").AsString().NotNullable()
            .WithColumn("Description").AsString().NotNullable()
            |> ignore

    member private this.CreateWineTable() =
        this.Create.Table("Wine")
            .WithColumn("ID").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("CategoryID").AsGuid().NotNullable()
            .WithColumn("Name").AsString().NotNullable()
            .WithColumn("Description").AsString().NotNullable()
            .WithColumn("Year").AsInt32().NotNullable()
            .WithColumn("Price").AsDecimal().NotNullable()
            .WithColumn("imagePath").AsString().NotNullable() |> ignore

        this.Create.ForeignKey("FK_Wine_Category")
            .FromTable("Wine").ForeignColumn("CategoryID")
            .ToTable("Category").PrimaryColumn("ID") |> ignore
        
    member private this.CreateWineInventory() =
        this.Create.Table("WineInventory")
            .WithColumn("ID").AsGuid().PrimaryKey()
            .WithColumn("WineID").AsGuid().NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable() |> ignore

        this.Create.ForeignKey("FK_WineInventory_Wine")
            .FromTable("WineInventory").ForeignColumn("WineID")
            .ToTable("Wine").PrimaryColumn("ID") |> ignore

    member private this.CreateUserTable() =
        this.Create.Table("User")
            .WithColumn("ID").AsGuid().PrimaryKey()
            .WithColumn("Email").AsString().NotNullable()
            .WithColumn("FirstName").AsString().NotNullable()
            .WithColumn("LastName").AsString().NotNullable()
            .WithColumn("Password").AsString().NotNullable()
            .WithColumn("Role").AsString().NotNullable() |> ignore

    member private this.CreateCartAndOrderTable() =
        this.Create.Table("Cart")
            .WithColumn("ID").AsGuid().PrimaryKey()
            .WithColumn("UserID").AsGuid().NotNullable() |> ignore

        this.Create.ForeignKey("FK_Cart_User")
            .FromTable("Cart").ForeignColumn("UserID")
            .ToTable("User").PrimaryColumn("ID") |> ignore

        this.Create.Table("CartItem")
            .WithColumn("ID").AsGuid().PrimaryKey()
            .WithColumn("CartID").AsGuid().NotNullable()
            .WithColumn("WineID").AsGuid().NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable() |> ignore

        this.Create.ForeignKey("FK_CartItem_Cart")
            .FromTable("CartItem").ForeignColumn("CartID")
            .ToTable("Cart").PrimaryColumn("ID") |> ignore

        this.Create.ForeignKey("FK_CartItem_Wine")
            .FromTable("CartItem").ForeignColumn("WineID")
            .ToTable("Wine").PrimaryColumn("ID") |> ignore

        this.Create.Table("Order")
            .WithColumn("ID").AsGuid().PrimaryKey()
            .WithColumn("CreatedDate").AsDateTime().NotNullable() |> ignore

        this.Create.Table("OrderItem")
            .WithColumn("ID").AsGuid().PrimaryKey()
            .WithColumn("OrderID").AsGuid().NotNullable()
            .WithColumn("WineID").AsGuid().NotNullable()
            .WithColumn("Price").AsDecimal().NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable() |> ignore

        this.Create.ForeignKey("FK_OrderItem_Order")
            .FromTable("OrderItem").ForeignColumn("OrderID")
            .ToTable("Order").PrimaryColumn("ID") |> ignore

        this.Create.ForeignKey("FK_OrderItem_Wine")
            .FromTable("OrderItem").ForeignColumn("WineID")
            .ToTable("Wine").PrimaryColumn("ID") |> ignore

    member private this.SeedDatabase () =
        let wineID1 = Guid("2a6c918595d94d8c80a6575f99c2a716")
        let wineID2 = Guid("699fb6e489774ab6ae892b7702556eba")
        let catID = Guid("4ec87f064d1e41b49342ab1aead1f99d")

        let wine1 = { Wine.id=wineID1; name="Edone Grand Cuvee Rose"; description="A Sample descripition that will be changed"; year=2014; price=14.4m; categoryId=catID; imagePath="img/Sparkling/grand-cuvee.jpg" }
        let wine2 = { Wine.id=wineID2; name="Raventós i Blanc de Nit"; description="A Sample description that will be changed"; year=2012; price=24.4m; categoryId=catID; imagePath="img/Sparkling/grand-cuvee.jpg" }

        // create and add list of categories
        let category = { id=catID; name="Sparkling"; description="A very fizzy type of wine with Champagne as a typical example."}

        // create list of users
        let admin = { id=catID; email="Admin"; firstName="Admin"; lastName="Admin"; role="admin"; password=BCrypt.Net.BCrypt.HashPassword("Admin") }

        this.Insert.IntoTable("Category").Row(category) |> ignore
        this.Insert.IntoTable("Wine").Row(wine1).Row(wine2) |> ignore
        this.Insert.IntoTable("User").Row(admin) |> ignore

