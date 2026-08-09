import { api } from "./api.js";
import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

const resetIcon = document.getElementById("resetIcon");
const resetIntroEyebrow = document.querySelector("#resetIntro .eyebrow");
const resetIntroHeading = document.querySelector("#resetIntro h2");
const resetIntroText = document.querySelector("#resetIntro p");
const missingTokenNotice = document.getElementById("missingTokenNotice");

const resetForm = document.getElementById("resetForm");
const newPasswordInput = document.getElementById("newPassword");
const confirmPasswordInput = document.getElementById("confirmPassword");
const newPasswordToggle = document.getElementById("newPasswordToggle");
const confirmPasswordToggle = document.getElementById("confirmPasswordToggle");
const newPasswordFeedback = document.getElementById("newPasswordFeedback");
const confirmPasswordFeedback = document.getElementById("confirmPasswordFeedback");
const resetButton = document.getElementById("resetButton");
const resetSuccess = document.getElementById("resetSuccess");

let isSubmitting = false;
let resetToken = null;

const CHECK_ICON =
    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round">
        <path class="auth-checkmark-path" d="M4 12.5L9.5 18L20 6" />
    </svg>`;

document.addEventListener("DOMContentLoaded", () => {
    renderNavbar();
    resetToken = new URLSearchParams(window.location.search).get("token");

    if (!resetToken) {
        if (missingTokenNotice) {
            missingTokenNotice.hidden = false;
        }

        if (resetForm) {
            resetForm.hidden = true;
        }

        return;
    }

    attachEventListeners();
});

function attachEventListeners() {
    if (resetForm) {
        resetForm.addEventListener("submit", handleResetPassword);
    }

    if (newPasswordToggle) {
        newPasswordToggle.addEventListener("click", () => {
            togglePasswordVisibility(newPasswordInput, newPasswordToggle);
        });
    }

    if (confirmPasswordToggle) {
        confirmPasswordToggle.addEventListener("click", () => {
            togglePasswordVisibility(confirmPasswordInput, confirmPasswordToggle);
        });
    }

    [newPasswordInput, confirmPasswordInput].forEach((input) => {
        if (!input) {
            return;
        }

        input.addEventListener("input", () => {
            clearFieldFeedback(input);
            hideFeedback();

            if (newPasswordInput.value && confirmPasswordInput.value) {
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

function getFeedbackElement(input) {
    if (input === newPasswordInput) {
        return newPasswordFeedback;
    }

    if (input === confirmPasswordInput) {
        return confirmPasswordFeedback;
    }

    return null;
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

function setFieldFeedback(input, message) {
    const feedback = getFeedbackElement(input);

    if (!feedback) {
        return;
    }

    input.classList.add("input-error");
    feedback.textContent = message;
    feedback.hidden = false;
}

function validatePasswordMatch() {
    const match = newPasswordInput.value === confirmPasswordInput.value;

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

    if (!newPasswordInput?.value) {
        setFieldFeedback(newPasswordInput, "New Password is required.");
        isValid = false;
    } else if (newPasswordInput.value.length < 8) {
        setFieldFeedback(newPasswordInput, "Password must be at least 8 characters.");
        isValid = false;
    }

    if (!confirmPasswordInput?.value) {
        setFieldFeedback(confirmPasswordInput, "Confirm Password is required.");
        isValid = false;
    }

    if (newPasswordInput?.value && confirmPasswordInput?.value) {
        if (!validatePasswordMatch()) {
            isValid = false;
        }
    }

    return isValid;
}

async function handleResetPassword(event) {
    event.preventDefault();

    if (isSubmitting) {
        return;
    }

    if (!validateForm()) {
        return;
    }

    hideFeedback();
    isSubmitting = true;
    setButtonLoading(resetButton, true, "Resetting...");

    try {
        const response = await api.post("/api/auth/reset-password", {
            Token: resetToken,
            NewPassword: newPasswordInput.value,
            ConfirmPassword: confirmPasswordInput.value
        });

        showSuccess(response?.message || "Password has been reset successfully.");
    } catch (error) {
        if (error?.data?.errors) {
            applyServerValidationErrors(error.data.errors);
            return;
        }

        showFeedback(
            error?.data?.message || error?.message || "Unable to reset your password. Please try again later.",
            "error"
        );
    } finally {
        isSubmitting = false;
        setButtonLoading(resetButton, false);
    }
}

function applyServerValidationErrors(errors) {
    let hasFieldError = false;

    if (Array.isArray(errors.NewPassword) && errors.NewPassword.length) {
        setFieldFeedback(newPasswordInput, errors.NewPassword[0]);
        hasFieldError = true;
    }

    if (Array.isArray(errors.ConfirmPassword) && errors.ConfirmPassword.length) {
        setFieldFeedback(confirmPasswordInput, errors.ConfirmPassword[0]);
        hasFieldError = true;
    }

    if (!hasFieldError) {
        showFeedback(
            (Array.isArray(errors.Token) && errors.Token[0]) ||
                "Please review the highlighted fields and try again.",
            "error"
        );
    }
}

function showSuccess(message) {
    if (resetForm) {
        resetForm.hidden = true;
    }

    if (resetIcon) {
        resetIcon.innerHTML = CHECK_ICON;
    }

    if (resetIntroEyebrow) {
        resetIntroEyebrow.textContent = "Success";
    }

    if (resetIntroHeading) {
        resetIntroHeading.textContent = "Password reset successfully.";
    }

    if (resetIntroText) {
        resetIntroText.textContent = message;
    }

    if (resetSuccess) {
        resetSuccess.hidden = false;
    }
}
