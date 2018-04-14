const domain = 'http://localhost:5000'
export const api = {
    login: `${domain}/api/auth/login`,
    signUp: `${domain}/api/auth/signup`,
    categories: `${domain}/api/categories`
}

export const keys = {
    token: "user:token",
    role: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
}