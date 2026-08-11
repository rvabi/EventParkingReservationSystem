import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback
} from "./ui.js";

import { buildEventPosterHtml } from "./event-poster.js";


const eventDetails =
    document.getElementById("eventDetails");


document.addEventListener(
    "DOMContentLoaded",
    async () => {

        renderNavbar();

        await loadEventDetails();
    }
);


async function loadEventDetails() {

    const parameters =
        new URLSearchParams(
            window.location.search
        );

    const eventId =
        Number(parameters.get("id"));


    if (!eventId || eventId <= 0) {

        showFeedback(
            "Invalid event selected.",
            "error"
        );

        return;
    }


    try {

        hideFeedback();

        const eventItem =
            await api.get(
                `/api/Events/${eventId}`
            );


        const venue =
            await api.get(
                `/api/Venues/${eventItem.venueId}`
            );


        renderEventDetails(
            eventItem,
            venue
        );

    } catch (error) {

        eventDetails.innerHTML = "";

        showFeedback(
            error.message ||
            "Unable to load event details.",
            "error"
        );
    }
}


/*
 * Category is intentionally not fetched or displayed here - see
 * Correction 5 (temporary frontend-only Category removal). The backend
 * EventCategory relationship and /api/Categories/{id} endpoint are
 * untouched; this page simply no longer calls them.
 */
function renderEventDetails(
    eventItem,
    venue
) {

    eventDetails.innerHTML = `
        <article class="event-details-card">

            <div class="event-details-hero">
                ${buildEventPosterHtml(eventItem, venue.name, "is-hero")}
            </div>

            <div class="event-details-body">

                <h2 class="event-details-title">
                    ${escapeHtml(eventItem.name)}
                </h2>

                <div class="event-details-grid">

                    <div class="event-details-item">
                        <span class="event-details-label">Venue</span>
                        <span class="event-details-value">${escapeHtml(venue.name)}</span>
                    </div>

                    <div class="event-details-item">
                        <span class="event-details-label">Date</span>
                        <span class="event-details-value">${formatDate(eventItem.startDateTime)}</span>
                    </div>

                    <div class="event-details-item">
                        <span class="event-details-label">Start Time &ndash; End Time</span>
                        <span class="event-details-value">
                            ${formatTime(eventItem.startDateTime)} &ndash; ${formatTime(eventItem.endDateTime)}
                        </span>
                    </div>

                    <div class="event-details-item">
                        <span class="event-details-label">Ticket Price</span>
                        <span class="event-details-value">LKR ${formatMoney(eventItem.ticketPrice)}</span>
                    </div>

                </div>

                <p class="event-details-description">
                    ${escapeHtml(
                        eventItem.description ||
                        "No description available."
                    )}
                </p>

                <div class="hero-actions">
                    <a
                        href="./seat-selection.html?id=${eventItem.id}"
                        id="selectSeatsButton"
                        class="btn btn-primary">
                        Select Seats
                    </a>
                </div>

            </div>

        </article>
    `;
}


function formatDate(value) {

    if (!value) {
        return "Not available";
    }

    return new Date(value)
        .toLocaleDateString();
}


function formatTime(value) {

    if (!value) {
        return "Not available";
    }

    return new Date(value)
        .toLocaleTimeString(
            [],
            {
                hour: "2-digit",
                minute: "2-digit"
            }
        );
}


function formatMoney(value) {

    const number =
        Number(value);

    if (Number.isNaN(number)) {
        return "0.00";
    }

    return number.toFixed(2);
}


function escapeHtml(value) {

    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
