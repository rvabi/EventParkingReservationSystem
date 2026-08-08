const TOKEN_KEY = "eventParkingAuthToken";

export function saveToken(token) {
    if (!token) {
        return;
    }

    localStorage.setItem(TOKEN_KEY, token);
}

export function getToken() {
    return localStorage.getItem(TOKEN_KEY);
}

export function removeToken() {
    localStorage.removeItem(TOKEN_KEY);
}

export function isAuthenticated() {
    return Boolean(getToken());
}