export type LoginModel = {
    userName: string
    password: string
}

export type UserModel = {
    email: string
    firstName: string
    lastName: string
    password: string
}

export type AuthResponse = {
    token: string
}

export enum Userstatus { loggedOut = -1, loggedIn = 1 }