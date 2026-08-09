import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";


const eventList =
    document.getElementById("eventList");

const eventNameInput =
    document.getElementById("eventName");

const eventDateInput =
    document.getElementById("eventDate");

const venueSelect =
    document.getElementById("venueId");

const categorySelect =
    document.getElementById("categoryId");

const searchButton =
    document.getElementById("searchEventsButton");

const clearButton =
    document.getElementById("clearFiltersButton");


let venues = [];
let categories = [];


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

        await Promise.all([
            loadVenues(),
            loadCategories()
        ]);

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


async function loadCategories() {
    categories =
        await api.get("/api/Categories");

    categorySelect.innerHTML = `
        <option value="">
            All Categories
        </option>
    `;

    categories.forEach((category) => {
        const option =
            document.createElement("option");

        option.value = category.id;
        option.textContent = category.name;

        categorySelect.appendChild(option);
    });
}


function hasActiveFilters() {
    return Boolean(
        eventNameInput.value.trim() ||
        eventDateInput.value ||
        venueSelect.value ||
        categorySelect.value
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

    const categoryName =
        getCategoryName(eventItem.eventCategoryId).toLowerCase();

    return (
        eventItem.name.toLowerCase().includes(searchText) ||
        venueName.includes(searchText) ||
        categoryName.includes(searchText)
    );
}


function buildEventQuery() {
    const parameters =
        new URLSearchParams();

    const date =
        eventDateInput.value;

    const venueId =
        venueSelect.value;

    const categoryId =
        categorySelect.value;

    /*
     * The "name" search box also matches venue and category
     * names, which the backend Events search does not support,
     * so text matching happens client-side in matchesSearchText.
     * Date/venue/category stay server-side since the backend
     * already combines them with AND semantics.
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

    if (categoryId) {
        parameters.append(
            "categoryId",
            categoryId
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

        const categoryName =
            getCategoryName(
                eventItem.eventCategoryId
            );

        const card =
            document.createElement("article");

        card.className =
            "service-card event-card";

        card.innerHTML = `
            <div class="service-number">
                #${eventItem.id}
            </div>

            <div class="service-icon">
                ${escapeHtml(categoryInitial(categoryName))}
            </div>

            <h3>
                ${escapeHtml(eventItem.name)}
            </h3>

            <p>
                ${escapeHtml(
                    eventItem.description ||
                    "No description available."
                )}
            </p>

            <p>
                <strong>Venue:</strong>
                ${escapeHtml(venueName)}
            </p>

            <p>
                <strong>Category:</strong>
                ${escapeHtml(categoryName)}
            </p>

            <p>
                <strong>Date:</strong>
                ${formatDate(
                    eventItem.startDateTime
                )}
            </p>

            <p>
                <strong>Time:</strong>
                ${formatTime(
                    eventItem.startDateTime
                )}
                -
                ${formatTime(
                    eventItem.endDateTime
                )}
            </p>

            <span class="service-link">
                Ticket: Rs.
                ${formatMoney(
                    eventItem.ticketPrice
                )}
            </span>
            <a
            href="./event-details.html?id=${eventItem.id}"
            class="btn btn-primary">
            View Details
            </a>
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


function categoryInitial(categoryName) {
    const trimmed = categoryName.trim();

    return trimmed
        ? trimmed.charAt(0).toUpperCase()
        : "E";
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


function getCategoryName(categoryId) {
    const category =
        categories.find(
            (item) =>
                item.id === categoryId
        );

    return category
        ? category.name
        : `Category ${categoryId}`;
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
        categorySelect.value = "";

        await loadEvents();
    }
);
