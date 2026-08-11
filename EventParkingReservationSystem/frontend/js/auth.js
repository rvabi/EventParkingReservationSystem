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

/*
 * Server-side controllers (e.g. BookingsController, FoodOrdersController)
 * derive the customer identity from the JWT itself, but FoodOrdersController
 * specifically requires the customerId to also be passed as a query
 * parameter (see the booking-checkout phase report for that backend gap).
 * Decoding the token client-side - rather than guessing an ID - is the only
 * safe way to supply it. Nothing here is used to bypass auth: the backend
 * still independently validates and authorizes every request.
 */
const ROLE_CLAIM_TYPE =
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

function decodeTokenPayload(token) {
    try {
        const base64Url = token.split(".")[1];
        const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");

        const json = decodeURIComponent(
            atob(base64)
                .split("")
                .map((char) =>
                    "%" + char.charCodeAt(0).toString(16).padStart(2, "0"))
                .join("")
        );

        return JSON.parse(json);
    } catch {
        return null;
    }
}

export function getCustomerId() {
    const token = getToken();

    if (!token) {
        return null;
    }

    const payload = decodeTokenPayload(token);
    const id = Number(payload?.sub);

    return Number.isFinite(id) && id > 0 ? id : null;
}

export function getCustomerRole() {
    const token = getToken();

    if (!token) {
        return null;
    }

    const payload = decodeTokenPayload(token);

    /*
     * JwtTokenService issues the role claim under the long
     * ClaimTypes.Role URI (confirmed by decoding a real generated token -
     * System.IdentityModel.Tokens.Jwt writes explicit Claim objects
     * verbatim, it does not shorten them). The short "role" fallback below
     * is defensive only, in case that ever changes; it is not currently
     * exercised by any real token this API issues.
     */
    return payload?.[ROLE_CLAIM_TYPE] || payload?.role || null;
}
