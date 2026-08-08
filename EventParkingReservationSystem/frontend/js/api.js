import { API_BASE_URL } from "./config.js";
import { getToken, removeToken } from "./auth.js";

async function parseResponse(response) {
    const text = await response.text();

    if (!text) {
        return null;
    }

    try {
        return JSON.parse(text);
    } catch {
        return text;
    }
}

export async function apiRequest(
    endpoint,
    options = {}) {

    const headers =
        new Headers(options.headers || {});

    const token = getToken();

    if (token) {
        headers.set(
            "Authorization",
            `Bearer ${token}`);
    }

    if (
        options.body &&
        !(options.body instanceof FormData) &&
        !headers.has("Content-Type")
    ) {
        headers.set(
            "Content-Type",
            "application/json");
    }

    try {
        const response = await fetch(
            `${API_BASE_URL}${endpoint}`,
            {
                ...options,
                headers
            });

        const data =
            await parseResponse(response);

        if (response.status === 401) {
            removeToken();
        }

        if (!response.ok) {
            const error = new Error(
                data?.message ||
                `Request failed with status ${response.status}.`
            );

            error.status = response.status;
            error.data = data;

            throw error;
        }

        return data;
    } catch (error) {
        if (error instanceof TypeError) {
            throw new Error(
                "Unable to connect to the API.");
        }

        throw error;
    }
}

export const api = {
    get(endpoint) {
        return apiRequest(
            endpoint,
            {
                method: "GET"
            });
    },

    post(endpoint, body) {
        return apiRequest(
            endpoint,
            {
                method: "POST",
                body: JSON.stringify(body)
            });
    },

    put(endpoint, body) {
        return apiRequest(
            endpoint,
            {
                method: "PUT",
                body: JSON.stringify(body)
            });
    },

    patch(endpoint, body) {
        return apiRequest(
            endpoint,
            {
                method: "PATCH",
                body: JSON.stringify(body)
            });
    },

    delete(endpoint) {
        return apiRequest(
            endpoint,
            {
                method: "DELETE"
            });
    }
};