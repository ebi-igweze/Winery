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
open System.Threading.Tasks
open Microsoft.AspNetCore.Http

// ---------------------------------
// Configure authentication
// ---------------------------------

let authOptions (options: AuthenticationOptions) =
    options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
    options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme

let jwtOptions (options: JwtBearerOptions) =
    // options.SaveToken <- true
    // options.IncludeErrorDetails <- true
    // options.Authority <- "https://ebi.igweze.com"
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

        this.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(Action<JwtBearerOptions> jwtOptions)  |> ignore
        this.AddSingleton(authService)                          |> ignore

    member this.AddWineryServices(env: IHostingEnvironment) =       
    
        // check with configurations later
        let isProduction = env.IsProduction()

        // get service depending on app environment
        let userQuery       = isProduction |> function | true -> FileStore.userQuery         | false -> DataStore.userQuery
        let cartQuery       = isProduction |> function | true -> FileStore.cartQuery         | false -> DataStore.cartQuery
        let wineQueries     = isProduction |> function | true -> FileStore.wineQueries       | false -> DataStore.wineQueries
        let categoryQueries = isProduction |> function | true -> FileStore.categoryQueries   | false -> DataStore.categoryQueries
        
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
        let wineCommandExecutioners     = isProduction |> function true -> FileStore.wineCommandExecutioners      | false -> DataStore.wineCommandExecutioners
        let userCommandExecutioners     = isProduction |> function true -> FileStore.userCommandExecutioners      | false -> DataStore.userCommandExecutioners
        let cartCommandExecutioner      = isProduction |> function true -> FileStore.cartCommandExecutioner       | false -> DataStore.cartCommandExecutioner
        let categoryCommandExecutioners = isProduction |> function true -> FileStore.categoryCommandExecutioners  | false -> DataStore.categoryCommandExecutioners

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
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app : IApplicationBuilder) =

    let serveIndexFile (ctx: HttpContext): Task = 
        task {
            if (ctx.Request.Path.Equals("/"))
            then ctx.Response.Redirect("/app-build/index.html")
            do Task.Yield() |> ignore
        } :> _
        
    do app.UseCors(configureCors)
          .UseStaticFiles()
          .UseAuthentication()
          .UseGiraffeErrorHandler(errorHandler)
          .UseGiraffe(webApp)

    do app.Run(RequestDelegate serveIndexFile)


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
    WebHostBuilder()
        .UseKestrel()
        .UseIISIntegration()
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0