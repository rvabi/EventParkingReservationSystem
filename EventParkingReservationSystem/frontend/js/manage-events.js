import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";


const eventForm =
    document.getElementById("eventForm");

const eventIdInput =
    document.getElementById("eventId");

const eventNameInput =
    document.getElementById("eventName");

const eventDescriptionInput =
    document.getElementById("eventDescription");

const venueIdInput =
    document.getElementById("venueId");

const categoryIdInput =
    document.getElementById("categoryId");

const startDateTimeInput =
    document.getElementById("startDateTime");

const endDateTimeInput =
    document.getElementById("endDateTime");

const eventCapacityInput =
    document.getElementById("eventCapacity");

const ticketPriceInput =
    document.getElementById("ticketPrice");

const parkingFeeInput =
    document.getElementById("parkingFee");

const eventList =
    document.getElementById("eventList");

const eventFormTitle =
    document.getElementById("eventFormTitle");

const saveEventButton =
    document.getElementById("saveEventButton");

const cancelEditButton =
    document.getElementById("cancelEditButton");


let events = [];
let venues = [];
let categories = [];


document.addEventListener(
    "DOMContentLoaded",
    async () => {

        renderNavbar();

        await initializePage();
    }
);


async function initializePage() {

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
            "Unable to initialize event management.",
            "error"
        );
    }
}


async function loadVenues() {

    venues =
        await api.get("/api/Venues");

    venueIdInput.innerHTML = `
        <option value="">
            Select Venue
        </option>
    `;

    venues.forEach((venue) => {

        const option =
            document.createElement("option");

        option.value =
            venue.id;

        option.textContent =
            `${venue.name} (Capacity: ${venue.totalCapacity})`;

        venueIdInput.appendChild(option);
    });
}


async function loadCategories() {

    categories =
        await api.get("/api/Categories");

    categoryIdInput.innerHTML = `
        <option value="">
            Select Category
        </option>
    `;

    categories.forEach((category) => {

        const option =
            document.createElement("option");

        option.value =
            category.id;

        option.textContent =
            category.name;

        categoryIdInput.appendChild(option);
    });
}


async function loadEvents() {

    try {

        events =
            await api.get("/api/Events");

        renderEvents();

    } catch (error) {

        eventList.innerHTML = "";

        showFeedback(
            error.message ||
            "Unable to load events.",
            "error"
        );
    }
}


function renderEvents() {

    eventList.innerHTML = "";

    if (!events || events.length === 0) {

        showFeedback(
            "No events are available.",
            "info"
        );

        return;
    }

    hideFeedback();


    events.forEach((event) => {

        const card =
            document.createElement("article");

        card.className =
            "service-card";

        card.innerHTML = `

            <div class="service-number">
                #${event.id}
            </div>

            <div class="service-icon">
                E
            </div>

            <h3>
                ${escapeHtml(event.name)}
            </h3>

            <p>
                ${escapeHtml(
                    event.description ||
                    "No description available."
                )}
            </p>

            <p>
                <strong>Venue:</strong>
                ${escapeHtml(
                    getVenueName(event.venueId)
                )}
            </p>

            <p>
                <strong>Category:</strong>
                ${escapeHtml(
                    getCategoryName(
                        event.eventCategoryId ??
                        event.categoryId
                    )
                )}
            </p>

            <p>
                <strong>Start:</strong>
                ${formatDateTime(
                    event.startDateTime
                )}
            </p>

            <p>
                <strong>End:</strong>
                ${formatDateTime(
                    event.endDateTime
                )}
            </p>

            <p>
                <strong>Capacity:</strong>
                ${event.capacity}
            </p>

            <p>
                <strong>Ticket Price:</strong>
                ${formatMoney(event.ticketPrice)}
            </p>

            <p>
                <strong>Parking Fee:</strong>
                ${formatMoney(event.parkingFee)}
            </p>


            <div
                class="hero-actions"
                style="margin-top: auto;">

                <button
                    class="btn btn-primary edit-event-button"
                    type="button"
                    data-id="${event.id}">
                    Edit
                </button>

                <button
                    class="btn btn-secondary delete-event-button"
                    type="button"
                    data-id="${event.id}">
                    Delete
                </button>

            </div>
        `;

        eventList.appendChild(card);
    });


    attachEventActionEvents();
}


function attachEventActionEvents() {

    document
        .querySelectorAll(
            ".edit-event-button"
        )
        .forEach((button) => {

            button.addEventListener(
                "click",
                () => {

                    startEditEvent(
                        Number(button.dataset.id)
                    );
                }
            );
        });


    document
        .querySelectorAll(
            ".delete-event-button"
        )
        .forEach((button) => {

            button.addEventListener(
                "click",
                async () => {

                    await deleteEvent(
                        Number(button.dataset.id)
                    );
                }
            );
        });
}


function startEditEvent(eventId) {

    const event =
        events.find(
            (item) =>
                item.id === eventId
        );

    if (!event) {
        return;
    }


    eventIdInput.value =
        event.id;

    eventNameInput.value =
        event.name;

    eventDescriptionInput.value =
        event.description || "";

    venueIdInput.value =
        event.venueId;

    categoryIdInput.value =
        event.eventCategoryId ??
        event.categoryId;

    startDateTimeInput.value =
        toDateTimeLocal(
            event.startDateTime
        );

    endDateTimeInput.value =
        toDateTimeLocal(
            event.endDateTime
        );

    eventCapacityInput.value =
        event.capacity;

    ticketPriceInput.value =
        event.ticketPrice;

    parkingFeeInput.value =
        event.parkingFee;


    eventFormTitle.textContent =
        "Edit Event";

    saveEventButton.textContent =
        "Update Event";

    cancelEditButton.hidden =
        false;


    eventForm.scrollIntoView({
        behavior: "smooth",
        block: "start"
    });
}


function resetEventForm() {

    eventForm.reset();

    eventIdInput.value = "";

    eventFormTitle.textContent =
        "Add New Event";

    saveEventButton.textContent =
        "Save Event";

    cancelEditButton.hidden =
        true;
}


async function saveEvent(event) {

    event.preventDefault();


    const eventId =
        eventIdInput.value;


    const eventData = {

        name:
            eventNameInput.value.trim(),

        description:
            eventDescriptionInput.value.trim(),

        venueId:
            Number(venueIdInput.value),

        eventCategoryId:
            Number(categoryIdInput.value),

        startDateTime:
            startDateTimeInput.value,

        endDateTime:
            endDateTimeInput.value,

        capacity:
            Number(eventCapacityInput.value),

        ticketPrice:
            Number(ticketPriceInput.value),

        parkingFee:
            Number(parkingFeeInput.value)
    };


    if (
        !eventData.name ||
        !eventData.description ||
        eventData.venueId <= 0 ||
        eventData.eventCategoryId <= 0 ||
        !eventData.startDateTime ||
        !eventData.endDateTime ||
        eventData.capacity <= 0 ||
        eventData.ticketPrice < 0 ||
        eventData.parkingFee < 0
    ) {

        showFeedback(
            "Please enter valid event details.",
            "error"
        );

        return;
    }


    if (
        new Date(eventData.endDateTime) <=
        new Date(eventData.startDateTime)
    ) {

        showFeedback(
            "End date and time must be after start date and time.",
            "error"
        );

        return;
    }


    try {

        setButtonLoading(
            saveEventButton,
            true,
            eventId
                ? "Updating..."
                : "Saving..."
        );

        hideFeedback();


        if (eventId) {

            await api.put(
                `/api/Events/${eventId}`,
                eventData
            );

            showFeedback(
                "Event updated successfully.",
                "success"
            );

        } else {

            await api.post(
                "/api/Events",
                eventData
            );

            showFeedback(
                "Event created successfully.",
                "success"
            );
        }


        resetEventForm();

        await loadEvents();

    } catch (error) {

        showFeedback(
            error.message ||
            "Unable to save event.",
            "error"
        );

    } finally {

        setButtonLoading(
            saveEventButton,
            false
        );
    }
}


async function deleteEvent(eventId) {

    const event =
        events.find(
            (item) =>
                item.id === eventId
        );

    if (!event) {
        return;
    }


    const confirmed =
        window.confirm(
            `Delete "${event.name}"?`
        );

    if (!confirmed) {
        return;
    }


    try {

        hideFeedback();

        await api.delete(
            `/api/Events/${eventId}`
        );

        showFeedback(
            "Event deleted successfully.",
            "success"
        );

        await loadEvents();

    } catch (error) {

        showFeedback(
            error.message ||
            "Unable to delete event.",
            "error"
        );
    }
}


function getVenueName(venueId) {

    const venue =
        venues.find(
            (item) =>
                item.id === venueId
        );

    return venue
        ? venue.name
        : `Venue #${venueId}`;
}


function getCategoryName(categoryId) {

    const category =
        categories.find(
            (item) =>
                item.id === categoryId
        );

    return category
        ? category.name
        : `Category #${categoryId}`;
}


function toDateTimeLocal(value) {

    if (!value) {
        return "";
    }

    return String(value)
        .slice(0, 16);
}


function formatDateTime(value) {

    if (!value) {
        return "-";
    }

    const date =
        new Date(value);

    return date.toLocaleString();
}


function formatMoney(value) {

    return Number(value ?? 0)
        .toFixed(2);
}


function escapeHtml(value) {

    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}


eventForm.addEventListener(
    "submit",
    saveEvent
);


cancelEditButton.addEventListener(
    "click",
    () => {

        resetEventForm();

        hideFeedback();
    }
);