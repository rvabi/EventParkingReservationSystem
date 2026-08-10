import { api } from "./api.js";
import { showFeedback, hideFeedback } from "./ui.js";
import { requireAdministrator, renderAdminSidebar, setupAdminMobileMenu } from "./admin-ui.js";


const adminShell = document.getElementById("adminShell");
const adminGreeting = document.getElementById("adminGreeting");
const summaryStrip = document.getElementById("summaryStrip");
const eventBoard = document.getElementById("eventBoard");

const MAX_BOARD_EVENTS = 8;

const SEATING_LAYOUT_LABEL = {
    StraightRows: "Hall / Straight",
    CircularArena: "Ground / Full Round"
};


document.addEventListener("DOMContentLoaded", async () => {
    if (!requireAdministrator()) {
        return;
    }

    renderAdminSidebar("dashboard");
    setupAdminMobileMenu();

    adminShell.hidden = false;
    adminGreeting.textContent = `${greetingForNow()}, Admin`;

    await initializePage();
});


function greetingForNow() {
    const hour = new Date().getHours();

    if (hour < 12) {
        return "Good morning";
    }

    if (hour < 17) {
        return "Good afternoon";
    }

    return "Good evening";
}


async function initializePage() {
    try {
        hideFeedback();

        const [events, venues, categories] = await Promise.all([
            api.get("/api/Events"),
            api.get("/api/Venues"),
            api.get("/api/Categories")
        ]);

        const bookings = await loadBookingsSafely();

        renderSummary(events, venues, categories, bookings);
        renderEventBoard(events, venues, categories, bookings);
    } catch (error) {
        showFeedback(
            error.message || "Unable to load the admin dashboard.",
            "error"
        );
    }
}


/*
 * GET /api/bookings is a real Administrator-only endpoint
 * (BookingsController.GetAll), but the dashboard must not show a fake
 * count if it can't be reached safely for any reason - so this fetch is
 * isolated from the rest of initializePage() and simply resolves to null
 * on failure instead of failing the whole dashboard load.
 */
async function loadBookingsSafely() {
    try {
        return await api.get("/api/bookings");
    } catch {
        return null;
    }
}


function renderSummary(events, venues, categories, bookings) {
    const upcomingCount = events.filter(isUpcoming).length;

    const items = [
        { label: "Upcoming Events", value: upcomingCount },
        { label: "Venues", value: venues.length },
        { label: "Categories", value: categories.length }
    ];

    if (Array.isArray(bookings)) {
        items.push({ label: "Bookings", value: bookings.length });
    }

    summaryStrip.innerHTML = items.map((item) => `
        <div class="admin-summary-item">
            <span class="admin-summary-label">${escapeHtml(item.label)}</span>
            <span class="admin-summary-value">${item.value}</span>
        </div>
    `).join("");

    summaryStrip.hidden = false;
}


function isUpcoming(eventItem) {
    return new Date(eventItem.startDateTime).getTime() >= Date.now();
}


function renderEventBoard(events, venues, categories, bookings) {
    const upcoming = events
        .filter(isUpcoming)
        .sort(
            (a, b) =>
                new Date(a.startDateTime).getTime() -
                new Date(b.startDateTime).getTime()
        )
        .slice(0, MAX_BOARD_EVENTS);

    if (!upcoming.length) {
        eventBoard.innerHTML = `
            <p class="admin-empty-note">
                No upcoming events. Use "Create Event" above to add one.
            </p>
        `;
        return;
    }

    const bookingCountByEvent = Array.isArray(bookings)
        ? bookings.reduce((map, booking) => {
            map.set(booking.eventId, (map.get(booking.eventId) || 0) + 1);
            return map;
        }, new Map())
        : null;

    eventBoard.innerHTML = upcoming.map((eventItem) => {
        const venue = venues.find((item) => item.id === eventItem.venueId);
        const category = categories.find(
            (item) => item.id === (eventItem.eventCategoryId ?? eventItem.categoryId)
        );

        const layoutLabel =
            SEATING_LAYOUT_LABEL[eventItem.seatingLayoutType] || "Hall / Straight";

        const bookingCount = bookingCountByEvent
            ? bookingCountByEvent.get(eventItem.id) || 0
            : null;

        return `
            <article class="admin-event-card">

                <h3 class="admin-event-card-title">${escapeHtml(eventItem.name)}</h3>
                <p class="admin-event-card-meta">
                    ${escapeHtml(category ? category.name : `Category #${eventItem.eventCategoryId ?? eventItem.categoryId}`)}
                </p>

                <div class="admin-event-card-detail-grid">

                    <div>
                        <span class="admin-event-card-detail-label">Venue</span>
                        <span class="admin-event-card-detail-value">
                            ${escapeHtml(venue ? venue.name : `Venue #${eventItem.venueId}`)}
                        </span>
                    </div>

                    <div>
                        <span class="admin-event-card-detail-label">Date</span>
                        <span class="admin-event-card-detail-value">
                            ${formatDate(eventItem.startDateTime)}
                        </span>
                    </div>

                    <div>
                        <span class="admin-event-card-detail-label">Time</span>
                        <span class="admin-event-card-detail-value">
                            ${formatTime(eventItem.startDateTime)}
                        </span>
                    </div>

                    <div>
                        <span class="admin-event-card-detail-label">Capacity</span>
                        <span class="admin-event-card-detail-value">${eventItem.capacity}</span>
                    </div>

                    <div>
                        <span class="admin-event-card-detail-label">Ticket Price</span>
                        <span class="admin-event-card-detail-value">${formatMoney(eventItem.ticketPrice)}</span>
                    </div>

                    ${
                        bookingCount !== null
                            ? `
                                <div>
                                    <span class="admin-event-card-detail-label">Bookings</span>
                                    <span class="admin-event-card-detail-value">${bookingCount}</span>
                                </div>
                              `
                            : ""
                    }

                </div>

                <span class="admin-layout-chip">${escapeHtml(layoutLabel)}</span>

                <div class="admin-event-card-actions">
                    <a href="./manage-events.html?edit=${eventItem.id}" class="btn btn-primary btn-small">
                        Manage Event
                    </a>
                    <a href="./manage-seats.html?id=${eventItem.id}" class="btn btn-secondary btn-small">
                        Manage Seats
                    </a>
                    <a href="./manage-parking.html?id=${eventItem.id}" class="btn btn-secondary btn-small">
                        Manage Parking
                    </a>
                    <a href="./manage-food.html?id=${eventItem.id}" class="btn btn-secondary btn-small">
                        Manage Food
                    </a>
                </div>

            </article>
        `;
    }).join("");
}


function formatDate(value) {
    if (!value) {
        return "-";
    }

    return new Date(value).toLocaleDateString();
}


function formatTime(value) {
    if (!value) {
        return "-";
    }

    return new Date(value).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}


function formatMoney(value) {
    const number = Number(value);
    return `LKR ${(Number.isNaN(number) ? 0 : number).toFixed(2)}`;
}


function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
