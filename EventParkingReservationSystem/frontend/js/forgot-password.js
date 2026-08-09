import { api } from "./api.js";
import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

const forgotForm = document.getElementById("forgotForm");
const emailInput = document.getElementById("email");
const emailFeedback = document.getElementById("emailFeedback");
const forgotButton = document.getElementById("forgotButton");
const devHint = document.getElementById("devHint");

let isSubmitting = false;

document.addEventListener("DOMContentLoaded", () => {
    renderNavbar();
    attachEventListeners();
});

function attachEventListeners() {
    if (forgotForm) {
        forgotForm.addEventListener("submit", handleForgotPassword);
    }

    if (emailInput) {
        emailInput.addEventListener("input", () => {
            clearFieldFeedback();
            hideFeedback();
        });
    }
}

function validateEmail(value) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function clearFieldFeedback() {
    if (!emailFeedback) {
        return;
    }

    emailFeedback.hidden = true;
    emailFeedback.textContent = "";
    emailInput?.classList.remove("input-error");
}

function setFieldFeedback(message) {
    if (!emailFeedback) {
        return;
    }

    emailFeedback.textContent = message;
    emailFeedback.hidden = false;
    emailInput?.classList.add("input-error");
}

async function handleForgotPassword(event) {
    event.preventDefault();

    if (isSubmitting) {
        return;
    }

    hideFeedback();
    clearFieldFeedback();

    if (devHint) {
        devHint.hidden = true;
        devHint.innerHTML = "";
    }

    const email = emailInput?.value.trim();

    if (!email) {
        setFieldFeedback("Email is required.");
        return;
    }

    if (!validateEmail(email)) {
        setFieldFeedback("Please enter a valid email address.");
        return;
    }

    isSubmitting = true;
    setButtonLoading(forgotButton, true, "Sending...");

    try {
        const response = await api.post("/api/auth/forgot-password", {
            Email: email
        });

        showFeedback(
            response?.message ||
                "If an account exists for this email, password reset instructions have been generated.",
            "success"
        );

        if (response?.resetToken) {
            renderDevHint(response.resetToken);
        }
    } catch (error) {
        if (error?.data?.errors) {
            applyServerValidationErrors(error.data.errors);
            return;
        }

        showFeedback(
            error?.data?.message || error?.message || "Unable to send reset instructions. Please try again later.",
            "error"
        );
    } finally {
        isSubmitting = false;
        setButtonLoading(forgotButton, false);
    }
}

function applyServerValidationErrors(errors) {
    const messages = errors.Email;

    if (Array.isArray(messages) && messages.length) {
        setFieldFeedback(messages[0]);
        return;
    }

    showFeedback("Please review the highlighted fields and try again.", "error");
}

function renderDevHint(token) {
    if (!devHint) {
        return;
    }

    devHint.innerHTML = "";

    const label = document.createElement("strong");
    label.textContent = "Development only";

    const tokenLine = document.createElement("div");
    tokenLine.textContent = `Reset token: ${token}`;

    const link = document.createElement("a");
    link.href = `reset-password.html?token=${encodeURIComponent(token)}`;
    link.textContent = "Continue to Reset Password (dev)";

    devHint.append(label, tokenLine, link);
    devHint.hidden = false;
}
