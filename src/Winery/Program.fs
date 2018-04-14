module Winery.App

open System
open Giraffe
open Http.Auth
open Storage
open Http.Cart
open Http.Wines
open BCrypt.Net
open System.Text
open Http.Categories
open Services.Models
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Hosting
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Authentication
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Authentication.JwtBearer
open Services.Actors.Storage
open Akka.FSharp.Spawn
open Services.Actors.User
open Http.Users
open Microsoft.AspNetCore.Builder
open System.IO

// ---------------------------------
// Configure authentication
// ---------------------------------

let authOptions (options: AuthenticationOptions) =
    options.DefaultScheme               <- JwtBearerDefaults.AuthenticationScheme
    options.DefaultSignOutScheme        <- JwtBearerDefaults.AuthenticationScheme
    options.DefaultChallengeScheme      <- JwtBearerDefaults.AuthenticationScheme
    options.DefaultAuthenticateScheme   <- JwtBearerDefaults.AuthenticationScheme

let jwtOptions (options: JwtBearerOptions) =
    options.SaveToken <- true
    options.TokenValidationParameters <- TokenValidationParameters (
        ValidateActor = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "ebi.igweze.com",
        ValidAudience = "http://localhost:5000/",
        IssuerSigningKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)) )

// ---------------------------------
// Register app services
// ---------------------------------
type IServiceCollection with

    member this.AddAuth() = 
        let authService: AuthService = 
            { hashPassword = BCrypt.HashPassword; verify = BCrypt.Verify }

        this.AddAuthentication(authOptions)
            .AddJwtBearer(Action<JwtBearerOptions> jwtOptions)  |> ignore
        this.AddSingleton(authService)                          |> ignore

    member this.AddWineryServices(env: IHostingEnvironment) =       
    
        // check with configurations later
        let isProduction = env.IsProduction()

        // get service depending on app environment
        let userQuery       = isProduction |> function | true -> FileStore.userQuery         | false -> InMemory.userQuery
        let cartQuery       = isProduction |> function | true -> FileStore.cartQuery         | false -> InMemory.cartQuery
        let wineQueries     = isProduction |> function | true -> FileStore.wineQueries       | false -> InMemory.wineQueries
        let categoryQueries = isProduction |> function | true -> FileStore.categoryQueries   | false -> InMemory.categoryQueries
        
        this.AddSingleton(userQuery)        |> ignore
        this.AddSingleton(cartQuery)        |> ignore
        this.AddMessageReceivers(env)       |> ignore
        this.AddSingleton(wineQueries)      |> ignore
        this.AddSingleton(categoryQueries)  |> ignore
        
    member private this.AddMessageReceivers(env: IHostingEnvironment) =
        // create actor system
        let system = Akka.FSharp.System.create "winery-system" (Akka.FSharp.Configuration.defaultConfig())
        
        // check with configuration later
        let isProduction = env.IsProduction()

        // get command services depending on app environment
        let wineCommandExecutioners     = isProduction |> function true -> FileStore.wineCommandExecutioners      | false -> InMemory.wineCommandExecutioners
        let userCommandExecutioners     = isProduction |> function true -> FileStore.userCommandExecutioners      | false -> InMemory.userCommandExecutioners
        let cartCommandExecutioner      = isProduction |> function true -> FileStore.cartCommandExecutioner       | false -> InMemory.cartCommandExecutioner
        let categoryCommandExecutioners = isProduction |> function true -> FileStore.categoryCommandExecutioners  | false -> InMemory.categoryCommandExecutioners

        // create actor refs
        let wineActorRef     = spawn system "wineActor" (wineActor wineCommandExecutioners)
        let userActorRef     = spawn system "userActor" (userActor userCommandExecutioners)
        let cartActorRef     = spawn system "cartActor" (cartActor cartCommandExecutioner)
        let commandActorRef  = spawn system "commandActor" commandActor
        let categoryActorRef = spawn system "categoryActor" (categoryActor categoryCommandExecutioners)

        // actor message receivers
        let cartReceiver       =  getCartReceiver cartActorRef
        let commandAgent       =  getCommandAgent commandActorRef
        let userReceivers      =  getUserReceivers userActorRef
        let wineReceivers      =  getWineReceivers wineActorRef
        let categoryReceivers  =  getCategoryReceivers categoryActorRef
        
        this.AddSingleton(cartReceiver)      |> ignore
        this.AddSingleton(commandAgent)      |> ignore
        this.AddSingleton(userReceivers)     |> ignore
        this.AddSingleton(wineReceivers)     |> ignore
        this.AddSingleton(categoryReceivers) |> ignore


// ---------------------------------
// Web app
// ---------------------------------

let webApp =
    choose [
        subRouteCi "/api"
            (choose [
                categoryHttpHandlers
                wineHttpHandlers
                cartHttpHandlers
                authHttpHandlers
                userHttpHandlers
            ])
        route "/" >=> redirectTo true "/index.html"
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins([| "http://localhost:8080"; "http://localhost:4200" |])
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app : IApplicationBuilder) =
    do app.UseCors(configureCors)
          .UseStaticFiles()
          .UseAuthentication()
          .UseGiraffeErrorHandler(errorHandler)
          .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) = 
    let sp = services.BuildServiceProvider()
    let env = sp.GetService<IHostingEnvironment>()
    
    services.AddCors()              |> ignore
    services.AddAuth()              |> ignore
    services.AddGiraffe()           |> ignore
    services.AddWineryServices(env) |> ignore

let configureLogging (builder : ILoggingBuilder) =
    let filter (l : LogLevel) = l.Equals LogLevel.Error
    builder.AddFilter(filter).AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main _ =
    let root = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(root, "wwwroot/app-build")

    WebHostBuilder()
        .UseContentRoot(root)
        .UseKestrel()
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0