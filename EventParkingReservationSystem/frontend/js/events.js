import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

import { buildEventPosterHtml } from "./event-poster.js";


const eventList =
    document.getElementById("eventList");

const eventNameInput =
    document.getElementById("eventName");

const eventDateInput =
    document.getElementById("eventDate");

const venueSelect =
    document.getElementById("venueId");

const searchButton =
    document.getElementById("searchEventsButton");

const clearButton =
    document.getElementById("clearFiltersButton");


let venues = [];


document.addEventListener(
    "DOMContentLoaded",
    async () => {
        renderNavbar();

        await initializeEventsPage();
    }
);


async function initializeEventsPage() {
    try {
        hideFeedback();

        await loadVenues();

        await loadEvents();
    } catch (error) {
        showFeedback(
            error.message ||
            "Unable to load event information.",
            "error"
        );
    }
}


async function loadVenues() {
    venues =
        await api.get("/api/Venues");

    venueSelect.innerHTML = `
        <option value="">
            All Venues
        </option>
    `;

    venues.forEach((venue) => {
        const option =
            document.createElement("option");

        option.value = venue.id;
        option.textContent = venue.name;

        venueSelect.appendChild(option);
    });
}


function hasActiveFilters() {
    return Boolean(
        eventNameInput.value.trim() ||
        eventDateInput.value ||
        venueSelect.value
    );
}


async function loadEvents() {
    try {
        setButtonLoading(
            searchButton,
            true,
            "Searching..."
        );

        hideFeedback();

        eventList.innerHTML = `
            <p class="event-list-status">
                Loading events...
            </p>
        `;

        const query =
            buildEventQuery();

        const events =
            await api.get(
                `/api/Events${query}`
            );

        const searchText =
            eventNameInput.value.trim().toLowerCase();

        const filteredEvents =
            searchText
                ? events.filter((eventItem) =>
                    matchesSearchText(eventItem, searchText))
                : events;

        renderEvents(filteredEvents);
    } catch (error) {
        eventList.innerHTML = "";

        showFeedback(
            error.message ||
            "Unable to load events.",
            "error"
        );
    } finally {
        setButtonLoading(
            searchButton,
            false
        );
    }
}


function matchesSearchText(eventItem, searchText) {
    const venueName =
        getVenueName(eventItem.venueId).toLowerCase();

    return (
        eventItem.name.toLowerCase().includes(searchText) ||
        venueName.includes(searchText)
    );
}


function buildEventQuery() {
    const parameters =
        new URLSearchParams();

    const date =
        eventDateInput.value;

    const venueId =
        venueSelect.value;

    /*
     * The "name" search box also matches venue names, which the backend
     * Events search does not support, so text matching happens
     * client-side in matchesSearchText. Date/venue stay server-side since
     * the backend already combines them with AND semantics. Category
     * filtering is intentionally not exposed here - see Correction 5
     * (temporary frontend-only Category removal); the backend
     * ?categoryId= param still works if this filter is reintroduced later.
     */

    if (date) {
        parameters.append(
            "date",
            date
        );
    }

    if (venueId) {
        parameters.append(
            "venueId",
            venueId
        );
    }


    const queryString =
        parameters.toString();

    return queryString
        ? `?${queryString}`
        : "";
}


function renderEvents(events) {
    eventList.innerHTML = "";

    if (!events || events.length === 0) {
        const message =
            hasActiveFilters()
                ? "No matching events found."
                : "There are no events available right now.";

        eventList.innerHTML = `
            <p class="event-list-status">
                ${escapeHtml(message)}
            </p>
        `;

        return;
    }

    events.forEach((eventItem) => {

        const venueName =
            getVenueName(
                eventItem.venueId
            );

        const card =
            document.createElement("article");

        card.className =
            "service-card event-card";

        card.innerHTML = `
            ${buildEventPosterHtml(eventItem, venueName, "is-card")}

            <div class="event-card-body">

                <h3 class="event-card-title">
                    ${escapeHtml(eventItem.name)}
                </h3>

                <p class="event-card-meta">
                    ${escapeHtml(venueName)}
                </p>

                <div class="event-card-detail-row">
                    <span>${formatDate(eventItem.startDateTime)}</span>
                    <span>
                        ${formatTime(eventItem.startDateTime)}
                        -
                        ${formatTime(eventItem.endDateTime)}
                    </span>
                </div>

                <div class="event-card-footer">
                    <span class="event-card-price">
                        LKR ${formatMoney(eventItem.ticketPrice)}
                    </span>
                    <a
                        href="./event-details.html?id=${eventItem.id}"
                        class="btn btn-primary btn-small">
                        View Details
                    </a>
                </div>

            </div>
        `;

        card.addEventListener("click", (event) => {
            if (event.target.closest("a")) {
                return;
            }

            window.location.href =
                `./event-details.html?id=${eventItem.id}`;
        });

        eventList.appendChild(card);
    });
}


function getVenueName(venueId) {
    const venue =
        venues.find(
            (item) =>
                item.id === venueId
        );

    return venue
        ? venue.name
        : `Venue ${venueId}`;
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
        return "";
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


searchButton.addEventListener(
    "click",
    loadEvents
);


eventNameInput.addEventListener(
    "keydown",
    (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            loadEvents();
        }
    }
);


clearButton.addEventListener(
    "click",
    async () => {
        eventNameInput.value = "";
        eventDateInput.value = "";
        venueSelect.value = "";

        await loadEvents();
    }
);
