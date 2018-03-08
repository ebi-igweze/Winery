module Http.Auth
open Giraffe
open System
open System.Text
open Storage.Models
open Services.Models
open System.Security.Claims
open Microsoft.AspNetCore.Http
open Microsoft.IdentityModel.Tokens
open System.IdentityModel.Tokens.Jwt
open Microsoft.AspNetCore.Authentication.JwtBearer
open Winery

// Handler to return 401 unauthorized challenge request
let private onError: HttpHandler = challenge JwtBearerDefaults.AuthenticationScheme

let internal finish : HttpFunc = Some >> Threading.Tasks.Task.FromResult

let authPolicy = (fun (user: ClaimsPrincipal) -> isNotNull user && user.Identity.IsAuthenticated)

// authroize if user is authenticated and is in specified role
let authorizePolicy role = fun u -> u |> authPolicy |> (&&) (u.IsInRole role)

// Handler to restrict access to unauthenticated users
let authenticate: HttpHandler = requiresAuthentication onError

let authorize role: HttpHandler = requiresAuthPolicy (authorizePolicy role) onError

let private requiresAuthPolicyWithArgs policy authFailedHandler (next : 'T -> HttpFunc)  =
    fun (args: 'T)  (ctx : HttpContext) ->
        if policy ctx.User
        then next args ctx
        else authFailedHandler finish ctx

let authenticateArgs func =  
    fun (args : 'T) (ctx : HttpContext) ->
        requiresAuthPolicyWithArgs authPolicy onError func args ctx

let authorizeArgs role func =
    fun (arg : 'T) (ctx : HttpContext) ->
        requiresAuthPolicyWithArgs (authorizePolicy role) onError func arg ctx        

// Handler to restrict access to only users with admin role
let authorizeAdmin: HttpHandler = authorize "admin"

// Handler to restrict access to only users with admin role
let authorizeAdminWithArgs func = authorizeArgs "admin" func

// Security key for signing tokens
let secret = "spadR2dre#u-ruBrE@TepA&*Uf@U"


type LoginModel =  
    { userName: string
      password: string }

type UserInfo = 
    { email: string
      firstName: string
      lastName: string 
      role: string }

type TokenResult = { token: string; }

type NewOrExisting = User of ExistingUser | NewUser of NewUser

let toUserInfo = function
    | NewUser user -> {email=user.email;firstName=user.firstName;lastName=user.lastName;role=(user.role.ToString())} 
    | User user -> {email=user.email;firstName=user.firstName;lastName=user.lastName;role=(user.role.ToString())}

let private generateToken user =
        let claims = 
            [|
                Claim(JwtRegisteredClaimNames.Sub, user.email)
                Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                Claim(ClaimTypes.Role, user.role)
            |]
        
        let notBefore = Nullable(DateTime.UtcNow)
        let expiresAt = Nullable(DateTime.UtcNow.AddDays(1.0))
        let securityKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        let credentials = SigningCredentials(key=securityKey, algorithm=SecurityAlgorithms.HmacSha256)

        let securityToken = 
            JwtSecurityToken(
                issuer = "ebi.igweze.com",
                audience = "http://localhost:5000/",
                claims = claims,
                expires = expiresAt,
                notBefore = notBefore,
                signingCredentials = credentials )

        { token = JwtSecurityTokenHandler().WriteToken(securityToken) }
    

let login: HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! model = ctx.BindJsonAsync<LoginModel>()
            let userQuery = ctx.GetService<UserQueries>()
            let authService = ctx.GetService<AuthService>()
            return! match userQuery.getUser (UserName model.userName) with 
                    | None -> unauthorizedM "UserName doesn't exist" next ctx
                    | Some (user, Password actualPassword) -> 
                        if (not (authService.verify (model.password, actualPassword))) 
                        then unauthorizedM "Invalid username or password" next ctx 
                        else User user |> toUserInfo |> generateToken |> json <|| (next, ctx)
        }

type SignUpModel = 
    { email: string
      firstName: string
      lastName: string
      password: string }

let signUp: HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! model = ctx.BindJsonAsync<SignUpModel>()
            let user = { NewUser.email=model.email; firstName=model.firstName; lastName=model.lastName; role=Customer }
            let userQuery = ctx.GetService<UserQueries>()
            let authService = ctx.GetService<AuthService>()
            let userCommands = ctx.GetService<UserCommands>()
            let hashedPassword = authService.hashPassword(model.password)
            return! match addUser userQuery.getUser userCommands.addUser (user, Password hashedPassword) with
                    | Error e -> handleError e next ctx
                    | Ok _ ->  NewUser user |> toUserInfo |> generateToken |> json <|| (next, ctx) 
        }

let logout: HttpHandler = signOut JwtBearerDefaults.AuthenticationScheme

let authHttpHandlers: HttpHandler = 
    (choose [
        routeCi "/login" >=> login
        routeCi "/signup" >=> signUp
        routeCi "/signout" >=> authenticate >=> logout
    ])