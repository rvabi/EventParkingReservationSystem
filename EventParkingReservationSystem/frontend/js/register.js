import { api } from "./api.js";
import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

const registerForm = document.getElementById("registerForm");
const fullNameInput = document.getElementById("fullName");
const emailInput = document.getElementById("email");
const phoneInput = document.getElementById("phone");
const passwordInput = document.getElementById("password");
const confirmPasswordInput = document.getElementById("confirmPassword");
const passwordToggle = document.getElementById("passwordToggle");
const confirmPasswordToggle = document.getElementById("confirmPasswordToggle");
const registerButton = document.getElementById("registerButton");
const registerSuccess = document.getElementById("registerSuccess");
const registerDevHint = document.getElementById("registerDevHint");

const fullNameFeedback = document.getElementById("fullNameFeedback");
const emailFeedback = document.getElementById("emailFeedback");
const phoneFeedback = document.getElementById("phoneFeedback");
const passwordFeedback = document.getElementById("passwordFeedback");
const confirmPasswordFeedback = document.getElementById("confirmPasswordFeedback");

let isSubmitting = false;

document.addEventListener("DOMContentLoaded", () => {
    renderNavbar();
    attachEventListeners();
});

function attachEventListeners() {
    if (registerForm) {
        registerForm.addEventListener("submit", handleRegister);
    }

    if (passwordToggle) {
        passwordToggle.addEventListener("click", () => {
            togglePasswordVisibility(passwordInput, passwordToggle);
        });
    }

    if (confirmPasswordToggle) {
        confirmPasswordToggle.addEventListener("click", () => {
            togglePasswordVisibility(confirmPasswordInput, confirmPasswordToggle);
        });
    }

    [fullNameInput, emailInput, phoneInput, passwordInput, confirmPasswordInput].forEach((input) => {
        if (!input) {
            return;
        }

        input.addEventListener("input", () => {
            clearFieldFeedback(input);
            hideFeedback();

            if (
                (input === passwordInput || input === confirmPasswordInput) &&
                passwordInput.value &&
                confirmPasswordInput.value
            ) {
                validatePasswordMatch();
            }
        });
    });
}

function togglePasswordVisibility(input, toggleButton) {
    if (!input || !toggleButton) {
        return;
    }

    const isVisible = input.type === "text";
    input.type = isVisible ? "password" : "text";
    toggleButton.textContent = isVisible ? "Show" : "Hide";
}

function clearFieldFeedback(input) {
    const feedback = getFeedbackElement(input);

    if (!feedback) {
        return;
    }

    feedback.hidden = true;
    feedback.textContent = "";
    input.classList.remove("input-error");
}

function getFeedbackElement(input) {
    switch (input) {
        case fullNameInput:
            return fullNameFeedback;
        case emailInput:
            return emailFeedback;
        case phoneInput:
            return phoneFeedback;
        case passwordInput:
            return passwordFeedback;
        case confirmPasswordInput:
            return confirmPasswordFeedback;
        default:
            return null;
    }
}

function setFieldFeedback(input, message) {
    const feedback = getFeedbackElement(input);

    if (!feedback) {
        return;
    }

    input.classList.add("input-error");
    feedback.textContent = message;
    feedback.hidden = false;
}

function validateEmail(value) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function validatePasswordMatch() {
    if (!passwordInput || !confirmPasswordInput) {
        return true;
    }

    const match = passwordInput.value === confirmPasswordInput.value;

    if (!match) {
        setFieldFeedback(confirmPasswordInput, "Passwords do not match.");
    } else {
        clearFieldFeedback(confirmPasswordInput);
    }

    return match;
}

function validateForm() {
    let isValid = true;

    hideFeedback();

    if (!fullNameInput?.value.trim()) {
        setFieldFeedback(fullNameInput, "Full Name is required.");
        isValid = false;
    }

    if (!emailInput?.value.trim()) {
        setFieldFeedback(emailInput, "Email is required.");
        isValid = false;
    } else if (!validateEmail(emailInput.value.trim())) {
        setFieldFeedback(emailInput, "Please enter a valid email address.");
        isValid = false;
    }

    if (!phoneInput?.value.trim()) {
        setFieldFeedback(phoneInput, "Phone is required.");
        isValid = false;
    }

    if (!passwordInput?.value) {
        setFieldFeedback(passwordInput, "Password is required.");
        isValid = false;
    } else if (passwordInput.value.length < 8) {
        setFieldFeedback(passwordInput, "Password must be at least 8 characters.");
        isValid = false;
    }

    if (!confirmPasswordInput?.value) {
        setFieldFeedback(confirmPasswordInput, "Confirm Password is required.");
        isValid = false;
    }

    if (passwordInput?.value && confirmPasswordInput?.value) {
        if (!validatePasswordMatch()) {
            isValid = false;
        }
    }

    return isValid;
}

async function handleRegister(event) {
    event.preventDefault();

    if (isSubmitting) {
        return;
    }

    if (!validateForm()) {
        return;
    }

    hideFeedback();
    isSubmitting = true;
    setButtonLoading(registerButton, true, "Creating account...");

    try {
        const response = await api.post("/api/auth/register", {
            FullName: fullNameInput.value.trim(),
            Email: emailInput.value.trim(),
            Phone: phoneInput.value.trim(),
            Password: passwordInput.value,
            ConfirmPassword: confirmPasswordInput.value
        });

        if (registerForm) {
            registerForm.hidden = true;
        }

        if (registerSuccess) {
            registerSuccess.hidden = false;
        }

        if (response?.verificationToken) {
            renderDevHint(response.verificationToken);
        }
    } catch (error) {
        if (error?.status === 409) {
            showFeedback(error.data?.message || "An account with this email already exists.", "error");
            return;
        }

        if (error?.data?.errors) {
            applyServerValidationErrors(error.data.errors);
            return;
        }

        showFeedback(
            error?.data?.message || error?.message || "Registration failed. Please try again.",
            "error"
        );
    } finally {
        isSubmitting = false;
        setButtonLoading(registerButton, false);
    }
}

function renderDevHint(token) {
    if (!registerDevHint) {
        return;
    }

    registerDevHint.innerHTML = "";

    const label = document.createElement("strong");
    label.textContent = "Development only";

    const tokenLine = document.createElement("div");
    tokenLine.textContent = `Verification token: ${token}`;

    const link = document.createElement("a");
    link.href = `verify-email.html?token=${encodeURIComponent(token)}`;
    link.textContent = "Verify now (dev)";

    registerDevHint.append(label, tokenLine, link);
    registerDevHint.hidden = false;
}

function applyServerValidationErrors(errors) {
    let hasFieldError = false;

    for (const key in errors) {
        const messages = errors[key];

        if (!Array.isArray(messages) || !messages.length) {
            continue;
        }

        const message = messages[0];

        switch (key) {
            case "FullName":
                setFieldFeedback(fullNameInput, message);
                hasFieldError = true;
                break;
            case "Email":
                setFieldFeedback(emailInput, message);
                hasFieldError = true;
                break;
            case "Phone":
                setFieldFeedback(phoneInput, message);
                hasFieldError = true;
                break;
            case "Password":
                setFieldFeedback(passwordInput, message);
                hasFieldError = true;
                break;
            case "ConfirmPassword":
                setFieldFeedback(confirmPasswordInput, message);
                hasFieldError = true;
                break;
            default:
                break;
        }
    }

    if (!hasFieldError) {
        showFeedback("Please review the highlighted fields and try again.", "error");
    }
}
