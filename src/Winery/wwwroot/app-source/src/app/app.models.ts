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

export type Category = {
    id: string,
    name: string,
    description: string
}

export class Wine {
    id: string
    name: string
    description: string
    year: number
    price: number
    imagePath: string
    categoryID: string
}

export class WineInventory extends Wine {
    quantity: number
}