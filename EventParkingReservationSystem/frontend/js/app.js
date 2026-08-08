import {
    renderNavbar,
    showFeedback
} from "./ui.js";

document.addEventListener(
    "DOMContentLoaded",
    () => {
        initializeApp();
    }
);

function initializeApp() {
    renderNavbar();

    showFeedback(
        "Smart Event platform is ready.",
        "success"
    );
}