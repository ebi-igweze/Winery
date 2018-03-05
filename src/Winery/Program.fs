module Winery.App

open System
open Giraffe
open Http.Auth
open Http.Cart
open Http.Wines
open BCrypt.Net
open System.Text
open Http.Categories
open Services.Models
open Storage.InMemory
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Authentication
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Authentication.JwtBearer

// ---------------------------------
// Configure authentication
// ---------------------------------

let authOptions (options: AuthenticationOptions) =
    options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
    options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme

let jwtOptions (options: JwtBearerOptions) =
    options.SaveToken <- true
    options.IncludeErrorDetails <- true
    options.Authority <- "https://ebi.igweze.com"
    options.TokenValidationParameters <- TokenValidationParameters (
        ValidIssuer = "ebi.igweze.com",
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidAudience = "localhost:5000",
        IssuerSigningKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)) )


// ---------------------------------
// Register app services
// ---------------------------------
type IServiceCollection with
    member this.AddAuth() = 
        let authService: AuthService = 
            { hashPassword = BCrypt.HashPassword; verify = BCrypt.Verify }

        this.AddSingleton(authService)                          |> ignore
        this.AddAuthentication(authOptions)
            .AddJwtBearer(Action<JwtBearerOptions> jwtOptions)  |> ignore

    member this.AddWineryServices() =
        this.AddSingleton(userQuery)          |> ignore
        this.AddSingleton(cartQuery)          |> ignore
        this.AddSingleton(cartCommand)        |> ignore
        this.AddSingleton(wineQueries)        |> ignore
        this.AddSingleton(wineCommands)       |> ignore
        this.AddSingleton(userCommands)       |> ignore
        this.AddSingleton(categoryQueries)    |> ignore
        this.AddSingleton(categoryCommands)   |> ignore

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
    let env = app.ApplicationServices.GetService<IHostingEnvironment>()
    do app.UseGiraffeErrorHandler(errorHandler)
        .UseCors(configureCors)
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()                          |> ignore
    services.AddAuth()                          |> ignore
    services.AddGiraffe()                       |> ignore
    services.AddWineryServices()                |> ignore

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