import { api } from "./api.js";
import {
    renderNavbar,
    setButtonLoading
} from "./ui.js";

const verifyLoading = document.getElementById("verifyLoading");
const verifyResult = document.getElementById("verifyResult");
const verifyResultIcon = document.getElementById("verifyResultIcon");
const verifyEyebrow = document.getElementById("verifyEyebrow");
const verifyHeading = document.getElementById("verifyHeading");
const verifyMessage = document.getElementById("verifyMessage");
const verifySuccessActions = document.getElementById("verifySuccessActions");
const resendSection = document.getElementById("resendSection");

const resendForm = document.getElementById("resendForm");
const resendEmailInput = document.getElementById("resendEmail");
const resendEmailFeedback = document.getElementById("resendEmailFeedback");
const resendFeedback = document.getElementById("resendFeedback");
const resendButton = document.getElementById("resendButton");
const resendDevHint = document.getElementById("resendDevHint");

let isResending = false;

const CHECK_ICON =
    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round">
        <path class="auth-checkmark-path" d="M4 12.5L9.5 18L20 6" />
    </svg>`;

const WARNING_ICON =
    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M12 3L22 20H2L12 3Z" />
        <line x1="12" y1="9" x2="12" y2="14" />
        <circle cx="12" cy="17.2" r="0.9" fill="currentColor" stroke="none" />
    </svg>`;

document.addEventListener("DOMContentLoaded", () => {
    renderNavbar();
    attachResendListener();
    runVerification();
});

function attachResendListener() {
    if (resendForm) {
        resendForm.addEventListener("submit", handleResend);
    }

    if (resendEmailInput) {
        resendEmailInput.addEventListener("input", () => {
            clearResendFieldFeedback();
            hideResendFeedback();
        });
    }
}

async function runVerification() {
    const token = new URLSearchParams(window.location.search).get("token");

    if (!token) {
        showResult({
            state: "invalid",
            message: "Verification token is required."
        });
        return;
    }

    try {
        const response = await api.get(
            `/api/auth/verify-email?token=${encodeURIComponent(token)}`
        );

        showResult({
            state: "success",
            message: response?.message || "Email verified successfully."
        });
    } catch (error) {
        if (error?.status === 400) {
            const message = error.data?.message || "Verification failed.";
            const state = message.toLowerCase().includes("expired") ? "expired" : "invalid";

            showResult({ state, message });
            return;
        }

        showResult({
            state: "error",
            message: error?.message || "Unable to verify your email right now. Please try again later."
        });
    }
}

function showResult({ state, message }) {
    if (verifyLoading) {
        verifyLoading.hidden = true;
    }

    if (verifyResult) {
        verifyResult.hidden = false;
    }

    const isSuccess = state === "success";

    if (verifyResultIcon) {
        verifyResultIcon.innerHTML = isSuccess ? CHECK_ICON : WARNING_ICON;
        verifyResultIcon.className = `auth-status-icon${isSuccess ? "" : " is-error"}`;
    }

    if (verifyEyebrow) {
        verifyEyebrow.textContent = isSuccess ? "Verified" : "Verification failed";
    }

    if (verifyHeading) {
        verifyHeading.textContent = isSuccess
            ? "Email verified successfully."
            : "We couldn't verify your email.";
    }

    if (verifyMessage) {
        verifyMessage.textContent = message;
    }

    if (verifySuccessActions) {
        verifySuccessActions.hidden = !isSuccess;
    }

    if (resendSection) {
        resendSection.hidden = isSuccess;
    }
}

function clearResendFieldFeedback() {
    if (!resendEmailFeedback) {
        return;
    }

    resendEmailFeedback.hidden = true;
    resendEmailFeedback.textContent = "";
    resendEmailInput?.classList.remove("input-error");
}

function hideResendFeedback() {
    if (!resendFeedback) {
        return;
    }

    resendFeedback.hidden = true;
    resendFeedback.textContent = "";
}

function showResendFeedback(message, type = "info") {
    if (!resendFeedback) {
        return;
    }

    resendFeedback.textContent = message;
    resendFeedback.className = `feedback feedback-${type}`;
    resendFeedback.hidden = false;
}

async function handleResend(event) {
    event.preventDefault();

    if (isResending) {
        return;
    }

    hideResendFeedback();
    clearResendFieldFeedback();

    if (resendDevHint) {
        resendDevHint.hidden = true;
        resendDevHint.innerHTML = "";
    }

    const email = resendEmailInput?.value.trim();

    if (!email) {
        resendEmailFeedback.textContent = "Email is required.";
        resendEmailFeedback.hidden = false;
        resendEmailInput?.classList.add("input-error");
        return;
    }

    isResending = true;
    setButtonLoading(resendButton, true, "Sending...");

    try {
        const response = await api.post("/api/auth/resend-verification", {
            Email: email
        });

        showResendFeedback(
            response?.message ||
                "If the account exists and requires verification, a new verification token has been generated.",
            "success"
        );

        if (response?.verificationToken) {
            renderDevHint(response.verificationToken);
        }
    } catch (error) {
        if (error?.data?.errors) {
            applyResendValidationErrors(error.data.errors);
            return;
        }

        showResendFeedback(
            error?.data?.message || error?.message || "Unable to resend verification email. Please try again later.",
            "error"
        );
    } finally {
        isResending = false;
        setButtonLoading(resendButton, false);
    }
}

function applyResendValidationErrors(errors) {
    const messages = errors.Email;

    if (Array.isArray(messages) && messages.length) {
        resendEmailFeedback.textContent = messages[0];
        resendEmailFeedback.hidden = false;
        resendEmailInput?.classList.add("input-error");
        return;
    }

    showResendFeedback("Please review the highlighted fields and try again.", "error");
}

function renderDevHint(token) {
    if (!resendDevHint) {
        return;
    }

    resendDevHint.innerHTML = "";

    const label = document.createElement("strong");
    label.textContent = "Development only";

    const tokenLine = document.createElement("div");
    tokenLine.textContent = `Token: ${token}`;

    const link = document.createElement("a");
    link.href = `verify-email.html?token=${encodeURIComponent(token)}`;
    link.textContent = "Verify now (dev)";

    resendDevHint.append(label, tokenLine, link);
    resendDevHint.hidden = false;
}
