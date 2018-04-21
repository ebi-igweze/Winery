const domain = 'http://localhost:5000'
export const api = {
    wineHub: `${domain}/hubs/wine`,
    login: `${domain}/api/auth/login`,
    signUp: `${domain}/api/auth/signup`,
    categories: `${domain}/api/categories`,
    allwines: `${domain}/api/categories/wines/all`,
    wines: (id) => `${domain}/api/categories/${id}/wines`
}

export const keys = {
    token: "user:token",
    role: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
}

export const copy = function<T>(c: T): T {
	let clone = <T> {};
	for (let key in c) {
		if (c.hasOwnProperty(key)) {
			let value = c[key];
			if (typeof value === 'object') clone[key] = copy(value);
			else clone[key] = value;
        }
    }
	return clone;
}