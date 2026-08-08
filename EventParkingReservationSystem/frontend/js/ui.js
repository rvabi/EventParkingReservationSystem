import {
    isAuthenticated,
    removeToken
} from "./auth.js";

/*
    Shared Navigation
*/
export function renderNavbar() {
    const navbar =
        document.getElementById("navbar");

    if (!navbar) {
        return;
    }

    const isInsidePages =
    window.location.pathname.includes("/pages/");

const homePath =
    isInsidePages
        ? "../index.html"
        : "./index.html";

    navbar.classList.add("navbar");

    navbar.innerHTML = `
        <div class="nav-container">

            <a
                href="${homePath}"
                class="brand">
                SmartEvent
            </a>

            <nav
                class="nav-links"
                aria-label="Main navigation">

                <a href="${homePath}">
                    Home
                </a>

                <a href="${homePath}#services">
                    Services
                </a>

                <a href="${homePath}#experience">
                    Experience
                </a>

                ${
                    isAuthenticated()
                        ? `
                            <button
                                id="logoutButton"
                                class="btn btn-secondary"
                                type="button">
                                Logout
                            </button>
                          `
                        : `
                            <span
                                class="btn btn-secondary"
                                title="Login will be connected after authentication module integration">
                                Login
                            </span>
                          `
                }

            </nav>

        </div>
    `;

    const logoutButton =
        document.getElementById(
            "logoutButton"
        );

    if (logoutButton) {
        logoutButton.addEventListener(
            "click",
            () => {
                removeToken();

                window.location.href =
                    homePath;
            }
        );
    }
}


/*
    Shared Feedback Message
*/
export function showFeedback(
    message,
    type = "info"
) {
    const feedback =
        document.getElementById(
            "globalFeedback"
        );

    if (!feedback) {
        return;
    }

    feedback.textContent = message;

    feedback.className =
        `feedback feedback-${type}`;

    feedback.hidden = false;
}


/*
    Hide Feedback Message
*/
export function hideFeedback() {
    const feedback =
        document.getElementById(
            "globalFeedback"
        );

    if (!feedback) {
        return;
    }

    feedback.hidden = true;
    feedback.textContent = "";
}


/*
    Shared Button Loading State
*/
export function setButtonLoading(
    button,
    isLoading,
    loadingText = "Please wait..."
) {
    if (!button) {
        return;
    }

    if (isLoading) {
        button.dataset.originalText =
            button.textContent;

        button.textContent =
            loadingText;

        button.disabled = true;

        return;
    }

    button.textContent =
        button.dataset.originalText ||
        button.textContent;

    button.disabled = false;
}