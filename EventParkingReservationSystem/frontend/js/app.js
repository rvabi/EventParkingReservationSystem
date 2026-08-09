import { api } from "./api.js";
import { renderNavbar } from "./ui.js";
import { isAuthenticated, getCustomerRole } from "./auth.js";

const FEATURED_EVENTS_LIMIT = 3;

const featuredEventsSection = document.getElementById("upcomingEventsSection");
const featuredEventsGrid = document.getElementById("featuredEventsGrid");

let venues = [];
let categories = [];


document.addEventListener("DOMContentLoaded", () => {
    initializeApp();
});


async function initializeApp() {
    renderNavbar();
    wireEntryPoints();
    await loadFeaturedEvents();
}


/*
 * Only unauthenticated visitors are sent to login.html - an already
 * signed-in customer clicking the same shortcuts should land on the real
 * destination instead of being asked to log in again. Administrators are
 * treated the same as any other authenticated user here; the public
 * homepage intentionally has no separate admin entry point.
 */
function eventsDestination() {
    return isAuthenticated()
        ? "./pages/events.html"
        : "./pages/login.html";
}

function notificationsDestination() {
    return isAuthenticated() && getCustomerRole() === "Customer"
        ? "./pages/notifications.html"
        : "./pages/login.html";
}

function eventDetailsDestination(eventId) {
    return isAuthenticated() && getCustomerRole() === "Customer"
        ? `./pages/event-details.html?id=${eventId}`
        : "./pages/login.html";
}


function wireEntryPoints() {
    document.querySelectorAll('[data-entry="events"]').forEach((element) => {
        element.href = eventsDestination();
    });

    document.querySelectorAll('[data-entry="notifications"]').forEach((element) => {
        element.href = notificationsDestination();
    });
}


async function loadFeaturedEvents() {
    if (!featuredEventsSection || !featuredEventsGrid) {
        return;
    }

    try {
        const [events] = await Promise.all([
            api.get("/api/Events"),
            loadLookupData()
        ]);

        const upcoming = (events || [])
            .filter((eventItem) => new Date(eventItem.startDateTime) > new Date())
            .sort((a, b) => new Date(a.startDateTime) - new Date(b.startDateTime))
            .slice(0, FEATURED_EVENTS_LIMIT);

        if (!upcoming.length) {
            return;
        }

        renderFeaturedEvents(upcoming);
        featuredEventsSection.hidden = false;
    } catch {
        // Featured events are a bonus preview, not core functionality -
        // fail quietly rather than showing an error on the public homepage.
    }
}


async function loadLookupData() {
    try {
        [venues, categories] = await Promise.all([
            api.get("/api/Venues"),
            api.get("/api/Categories")
        ]);
    } catch {
        venues = [];
        categories = [];
    }
}


function renderFeaturedEvents(events) {
    featuredEventsGrid.innerHTML = events.map((eventItem) => {
        const venueName = getVenueName(eventItem.venueId);
        const categoryName = getCategoryName(eventItem.eventCategoryId);
        const destination = eventDetailsDestination(eventItem.id);

        return `
            <a class="home-event-card" href="${destination}">
                <div class="home-event-icon">${escapeHtml(categoryInitial(categoryName))}</div>
                <span class="home-event-category">${escapeHtml(categoryName)}</span>
                <h3>${escapeHtml(eventItem.name)}</h3>
                <p class="home-event-meta">
                    ${formatDate(eventItem.startDateTime)} · ${escapeHtml(venueName)}
                </p>
                <span class="home-event-price">
                    From LKR ${formatMoney(eventItem.ticketPrice)}
                </span>
            </a>
        `;
    }).join("");
}


function categoryInitial(categoryName) {
    const trimmed = String(categoryName || "").trim();

    return trimmed ? trimmed.charAt(0).toUpperCase() : "E";
}


function getVenueName(venueId) {
    const venue = venues.find((item) => item.id === venueId);

    return venue ? venue.name : "Venue TBA";
}


function getCategoryName(categoryId) {
    const category = categories.find((item) => item.id === categoryId);

    return category ? category.name : "Event";
}


function formatDate(value) {
    if (!value) {
        return "Date TBA";
    }

    return new Date(value).toLocaleDateString(undefined, {
        day: "numeric",
        month: "short",
        year: "numeric"
    });
}


function formatMoney(value) {
    const number = Number(value);

    return (Number.isNaN(number) ? 0 : number).toFixed(2);
}


function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
