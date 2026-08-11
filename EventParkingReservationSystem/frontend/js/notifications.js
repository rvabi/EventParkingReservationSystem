import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback
} from "./ui.js";

import { isAuthenticated, getCustomerRole } from "./auth.js";


const authGuardPanel = document.getElementById("authGuardPanel");
const authGuardMessage = document.getElementById("authGuardMessage");
const authGuardLink = document.getElementById("authGuardLink");
const backLink = document.getElementById("backLink");
const notificationsList = document.getElementById("notificationsList");

/*
 * Frontend-only presentation labels for backend NotificationType values.
 * The backend Type field itself is never modified - this only controls
 * what heading text is shown. Anything not listed here falls back to a
 * PascalCase-to-words conversion (see humanizeType), so an unknown future
 * type never breaks rendering.
 */
const TYPE_LABELS = {
    General: "Update",
    BookingCreated: "Booking held",
    BookingConfirmed: "Booking confirmed",
    BookingCancelled: "Booking cancelled",
    BookingExpired: "Booking hold expired",
    PaymentCompleted: "Payment successful",
    EventUpdated: "Event updated",
    EventReminder: "Event reminder",
    FoodReady: "Food ready for pickup"
};

/*
 * Booking pages that a notification's "View Booking" action, or the
 * Notifications "Back" link, can safely resolve back to - with a distinct,
 * page-specific back label for each.
 */
const RETURN_PAGE_LABELS = {
    "seat-selection.html": "← Back to Seat Selection",
    "booking-summary.html": "← Back to Booking Summary",
    "booking-confirmation.html": "← Back to Booking Confirmation"
};

const BOOKING_NUMBER_PATTERN = /BKG-\d{4}-\d{6}/;

/*
 * Backend notification messages sometimes embed a raw ISO-8601 timestamp
 * (e.g. BookingCreated's "... held until 2026-08-10T00:09:31.62Z."). The
 * backend value itself is never changed - this only reformats how an
 * already-present ISO substring is displayed.
 */
const ISO_DATE_PATTERN = /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z?/g;

let bookingsByNumber = new Map();


document.addEventListener("DOMContentLoaded", async () => {
    renderNavbar();
    setUpBackLink();
    await initializePage();
});


function setUpBackLink() {
    const safeReturn = getSafeReturnUrl();

    if (!safeReturn) {
        backLink.href = "events.html";
        backLink.textContent = "← Back to Events";
        return;
    }

    backLink.href = safeReturn;

    const pageName = safeReturn.split("?")[0];

    backLink.textContent = RETURN_PAGE_LABELS[pageName] || "← Back";
}


/*
 * Only plain same-directory relative page URLs are ever accepted (for
 * example "booking-summary.html?id=2"). Anything carrying a scheme
 * (http:, https:, javascript:, data:, ...), a protocol-relative "//"
 * prefix, or a ".." path segment is rejected outright to avoid an open
 * redirect via a crafted notifications.html?return= link.
 */
function getSafeReturnUrl() {
    const raw = new URLSearchParams(window.location.search).get("return");

    if (!raw) {
        return null;
    }

    if (/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(raw)) {
        return null;
    }

    if (raw.startsWith("//") || raw.startsWith("\\") || raw.includes("..")) {
        return null;
    }

    const [pagePart, queryPart = ""] = raw.split("?");

    if (!/^[a-zA-Z0-9_-]+\.html$/.test(pagePart)) {
        return null;
    }

    if (queryPart && /[^a-zA-Z0-9=&%_-]/.test(queryPart)) {
        return null;
    }

    return raw;
}


async function initializePage() {
    if (!isAuthenticated()) {
        showAuthGuard(
            "Please log in with a customer account to view notifications.",
            "login.html"
        );
        return;
    }

    if (getCustomerRole() !== "Customer") {
        showAuthGuard(
            "Notifications are only available to customer accounts.",
            "manage-events.html"
        );
        return;
    }

    await loadNotifications();
}


function showAuthGuard(message, linkHref) {
    authGuardMessage.textContent = message;
    authGuardLink.href = linkHref;
    authGuardPanel.hidden = false;
}


async function loadNotifications() {
    try {
        hideFeedback();
        notificationsList.innerHTML = `<p class="event-list-status">Loading notifications...</p>`;

        const [notifications] = await Promise.all([
            api.get("/api/notifications/my-notifications"),
            loadMyBookingsForCorrelation()
        ]);

        renderNotifications(notifications);
    } catch (error) {
        notificationsList.innerHTML = "";

        if (error?.status === 401) {
            showAuthGuard(
                "Your session has expired. Please log in again.",
                "login.html"
            );
            return;
        }

        showFeedback(
            error.message || "Unable to load your notifications.",
            "error"
        );
    }
}


/*
 * Notification messages already contain the real BookingNumber (e.g.
 * "Booking BKG-2026-000002 is held until ..."), but not a BookingId. The
 * only backend-supported way to resolve BookingNumber -> BookingId is to
 * look it up in the customer's own real booking list - never parsed or
 * guessed from the formatted reference string itself.
 */
async function loadMyBookingsForCorrelation() {
    try {
        const bookings = await api.get("/api/bookings/my-bookings");

        bookingsByNumber = new Map(
            bookings.map((booking) => [booking.bookingNumber, booking])
        );
    } catch {
        bookingsByNumber = new Map();
    }
}


function renderNotifications(notifications) {
    if (!notifications || notifications.length === 0) {
        notificationsList.innerHTML =
            `<p class="event-list-status">You have no notifications yet.</p>`;
        return;
    }

    const sorted = notifications.slice().sort(
        (a, b) => new Date(b.createdAt) - new Date(a.createdAt)
    );

    notificationsList.innerHTML = sorted.map((notification) => {
        const booking = findRelatedBooking(notification.message);
        const isUnread = !notification.isRead;

        return `
            <article
                class="notification-card${isUnread ? " is-unread" : ""}"
                data-id="${notification.id}"
                ${isUnread ? 'tabindex="0" role="button" aria-label="Mark this notification as read"' : ""}>

                <div class="notification-card-top">
                    <span class="notification-dot" aria-hidden="true"></span>
                    <h4 class="notification-title">${escapeHtml(displayLabel(notification.type))}</h4>
                </div>

                <p class="notification-message">${formatMessage(notification.message)}</p>

                <p class="notification-created">Created: ${formatDateTime(notification.createdAt)}</p>

                <div class="notification-card-actions">
                    ${booking
                        ? `<a href="${bookingLinkFor(booking)}" class="btn btn-secondary btn-small">View Booking</a>`
                        : ""
                    }
                </div>
            </article>
        `;
    }).join("");

    attachCardInteractions();
}


function findRelatedBooking(message) {
    const match = String(message ?? "").match(BOOKING_NUMBER_PATTERN);

    if (!match) {
        return null;
    }

    return bookingsByNumber.get(match[0]) || null;
}


function bookingLinkFor(booking) {
    return booking.status === "Confirmed"
        ? `booking-confirmation.html?id=${booking.id}`
        : `booking-summary.html?id=${booking.id}`;
}


function displayLabel(type) {
    return TYPE_LABELS[type] || humanizeType(type);
}


function humanizeType(type) {
    const safe = String(type ?? "").trim();

    if (!safe) {
        return "Update";
    }

    return safe
        .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
        .replace(/^./, (char) => char.toUpperCase());
}


/*
 * Escapes the real backend message, then reformats any embedded raw
 * ISO-8601 timestamp into the same clean local format used elsewhere on
 * the page - never showing "2026-08-09T18:39:31.62Z" style text.
 */
function formatMessage(message) {
    return escapeHtml(message).replace(
        ISO_DATE_PATTERN,
        (match) => formatDateTime(match)
    );
}


/*
 * Marking as read on open/view rather than via a visible button: clicking
 * (or activating via keyboard) an unread card calls the same real
 * PUT /api/notifications/{id}/read endpoint the old button used, then
 * updates that card's state in place. No visible "Mark as read" control.
 */
function attachCardInteractions() {
    notificationsList.querySelectorAll(".notification-card.is-unread").forEach((card) => {
        const markRead = () => markNotificationAsRead(card);

        card.addEventListener("click", markRead, { once: true });

        card.addEventListener("keydown", (event) => {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                markRead();
            }
        }, { once: true });
    });
}


async function markNotificationAsRead(card) {
    const id = Number(card.dataset.id);

    card.classList.remove("is-unread");
    card.removeAttribute("tabindex");
    card.removeAttribute("role");
    card.removeAttribute("aria-label");

    try {
        await api.put(`/api/notifications/${id}/read`);
    } catch {
        // Best-effort: on failure the notification simply shows as unread
        // again on the next load, where it can be marked read again.
    }
}


function formatDateTime(value) {
    if (!value) {
        return "";
    }

    return new Date(value).toLocaleString(undefined, {
        day: "numeric",
        month: "short",
        year: "numeric",
        hour: "numeric",
        minute: "2-digit",
        hour12: true
    });
}


function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
