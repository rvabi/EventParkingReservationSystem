import { api } from "./api.js";
import { saveToken } from "./auth.js";
import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

const loginForm = document.getElementById("loginForm");
const emailInput = document.getElementById("email");
const passwordInput = document.getElementById("password");
const passwordToggle = document.getElementById("passwordToggle");
const rememberMe = document.getElementById("rememberMe");
const loginButton = document.getElementById("loginButton");

document.addEventListener("DOMContentLoaded", () => {
    renderNavbar();
    attachEventListeners();
});

function attachEventListeners() {
    if (loginForm) {
        loginForm.addEventListener("submit", handleLogin);
    }

    if (passwordToggle) {
        passwordToggle.addEventListener(
            "click",
            togglePasswordVisibility
        );
    }
}

function togglePasswordVisibility() {
    const isPasswordVisible =
        passwordInput.type === "text";

    passwordInput.type = isPasswordVisible
        ? "password"
        : "text";

    passwordToggle.textContent = isPasswordVisible
        ? "Show"
        : "Hide";
}

async function handleLogin(event) {
    event.preventDefault();

    hideFeedback();

    const email = emailInput.value.trim();
    const password = passwordInput.value;
    const remember = rememberMe.checked;

    if (!email || !password) {
        showFeedback(
            "Please provide both email and password.",
            "error"
        );

        return;
    }

    setButtonLoading(loginButton, true, "Signing in...");

    try {
        const response = await api.post("/api/auth/login", {
            email,
            password
        });

        const loginData = response?.data;

        if (!loginData?.token) {
            throw new Error(
                "Login succeeded but no token was returned."
            );
        }

        saveToken(loginData.token, remember);

        const destination =
            loginData.role === "Administrator"
                ? "admin-dashboard.html"
                : "events.html";

        window.location.assign(destination);
    } catch (error) {
        const message =
            error?.data?.message ||
            error?.message ||
            "Unable to sign in. Please try again later.";

        showFeedback(message, "error");
        setButtonLoading(loginButton, false);
    }
}
