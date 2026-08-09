import {
    isAuthenticated,
    getCustomerRole,
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

const loginPath =
    isInsidePages
        ? "./login.html"
        : "./pages/login.html";

const notificationsPath =
    isInsidePages
        ? "./notifications.html"
        : "./pages/notifications.html";

/*
 * Carries the current page as a safe return target so Notifications can
 * offer a "Back to Booking" action. Only ever built from the browser's
 * own trusted location (never from user input), then read back and
 * re-validated by notifications.js before use.
 */
const currentFile =
    isInsidePages
        ? window.location.pathname.split("/").pop()
        : "";

const notificationsHref =
    currentFile && currentFile !== "notifications.html"
        ? `${notificationsPath}?return=${encodeURIComponent(currentFile + window.location.search)}`
        : notificationsPath;

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

                <a href="${homePath}#contact">
                    Contact
                </a>

                ${
                    isAuthenticated() && getCustomerRole() === "Customer"
                        ? `
                            <a href="${notificationsHref}">
                                Notifications
                            </a>
                          `
                        : ""
                }

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
                            <a
                                href="${loginPath}"
                                class="btn btn-secondary">
                                Login
                            </a>
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