module Services.Models

type AuthService =
    { hashPassword: string -> string 
      verify: string * string -> bool }