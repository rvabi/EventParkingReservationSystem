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


async function loadEvents() {
    try {
        setButtonLoading(
            searchButton,
            true,
            "Searching..."
        );

        hideFeedback();

        const query =
            buildEventQuery();

        const events =
            await api.get(
                `/api/Events${query}`
            );

        renderEvents(events);
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


function buildEventQuery() {
    const parameters =
        new URLSearchParams();

    const name =
        eventNameInput.value.trim();

    const date =
        eventDateInput.value;

    const venueId =
        venueSelect.value;

    const categoryId =
        categorySelect.value;


    if (name) {
        parameters.append(
            "name",
            name
        );
    }

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
        showFeedback(
            "No events found for the selected filters.",
            "info"
        );

        return;
    }

    hideFeedback();

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
            "service-card";

        card.innerHTML = `
            <div class="service-number">
                #${eventItem.id}
            </div>

            <div class="service-icon">
                E
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