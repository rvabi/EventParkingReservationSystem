const TOKEN_KEY = "eventParkingAuthToken";

export function saveToken(token, remember = true) {
    if (!token) {
        return;
    }

    if (remember) {
        localStorage.setItem(TOKEN_KEY, token);
        sessionStorage.removeItem(TOKEN_KEY);
        return;
    }

    sessionStorage.setItem(TOKEN_KEY, token);
    localStorage.removeItem(TOKEN_KEY);
}

export function getToken() {
    return (
        localStorage.getItem(TOKEN_KEY) ||
        sessionStorage.getItem(TOKEN_KEY)
    );
}

export function removeToken() {
    localStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(TOKEN_KEY);
}

export function isAuthenticated() {
    return Boolean(getToken());
}
