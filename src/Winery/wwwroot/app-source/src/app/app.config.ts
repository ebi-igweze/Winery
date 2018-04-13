const domain = 'http://localhost:5000'
export const api = {
    login: `${domain}/api/auth/login`,
    signUp: `${domain}/api/auth/signup`,
    logout: `${domain}/api/auth/signout`,
    categories: `${domain}/api/categories`
}

export const keys = {
    token: "user:token"
}