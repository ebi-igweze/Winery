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

export type Command = { href: string }

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

// declare global {
//     interface Object {
//         update: (item: Object) => void;
//     } 
// }

// Object.prototype.update = function (item: Object) {
//     if (item) {
//         for (let key in item) {
//             if (item.hasOwnProperty(key))
//                 this[key] = item[key];
//         }
//     }
// }
