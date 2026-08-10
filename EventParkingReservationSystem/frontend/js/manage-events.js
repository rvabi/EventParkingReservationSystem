import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

import { isAuthenticated, getCustomerRole } from "./auth.js";


/*
 * SeatingLayoutType is persisted on the backend as an int (StraightRows = 1,
 * CircularArena = 2) but GET Events returns it as the enum member's string
 * name ("StraightRows" / "CircularArena") - see EventDto.SeatingLayoutType.
 * These two maps keep that conversion in one place instead of scattering
 * magic numbers/strings through the wizard.
 */
const SEATING_LAYOUT_TYPE = {
    STRAIGHT_ROWS: 1,
    CIRCULAR_ARENA: 2
};

const SEATING_LAYOUT_NAME_TO_VALUE = {
    StraightRows: SEATING_LAYOUT_TYPE.STRAIGHT_ROWS,
    CircularArena: SEATING_LAYOUT_TYPE.CIRCULAR_ARENA
};

const SEATING_LAYOUT_VALUE_TO_LABEL = {
    [SEATING_LAYOUT_TYPE.STRAIGHT_ROWS]: "Hall / Straight",
    [SEATING_LAYOUT_TYPE.CIRCULAR_ARENA]: "Ground / Full Round"
};


const authGuardPanel = document.getElementById("authGuardPanel");
const authGuardMessage = document.getElementById("authGuardMessage");
const authGuardLink = document.getElementById("authGuardLink");

const eventWizardCard = document.getElementById("eventWizardCard");

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

const layoutInputs =
    Array.from(document.querySelectorAll('input[name="seatingLayoutType"]'));

const eventList =
    document.getElementById("eventList");

const eventFormTitle =
    document.getElementById("eventFormTitle");

const saveEventButton =
    document.getElementById("saveEventButton");

const cancelEditButton =
    document.getElementById("cancelEditButton");

const wizardBackButton = document.getElementById("wizardBackButton");
const wizardNextButton = document.getElementById("wizardNextButton");
const wizardFeedback = document.getElementById("wizardFeedback");

const stepPanels = Array.from(document.querySelectorAll(".wizard-step-panel"));
const stepIndicators = Array.from(document.querySelectorAll(".wizard-step-indicator"));

const TOTAL_STEPS = stepPanels.length;
let currentStep = 1;


let events = [];
let venues = [];
let categories = [];


document.addEventListener(
    "DOMContentLoaded",
    async () => {

        renderNavbar();

        if (!isAuthenticated()) {
            showAuthGuard(
                "Please log in with an administrator account to manage events.",
                "login.html"
            );
            return;
        }

        if (getCustomerRole() !== "Administrator") {
            showAuthGuard(
                "Event management is only available to administrator accounts.",
                "login.html"
            );
            return;
        }

        eventWizardCard.hidden = false;

        await initializePage();
    }
);


function showAuthGuard(message, linkHref) {
    authGuardMessage.textContent = message;
    authGuardLink.href = linkHref;
    authGuardPanel.hidden = false;
}


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

        const layoutLabel =
            layoutLabelFromName(event.seatingLayoutType);

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

            <span class="layout-badge">${escapeHtml(layoutLabel)}</span>


            <div
                class="hero-actions"
                style="margin-top: auto;">

                <button
                    class="btn btn-primary edit-event-button"
                    type="button"
                    data-id="${event.id}">
                    Edit
                </button>

                <a
                    href="./manage-seats.html?id=${event.id}"
                    class="btn btn-secondary">
                    Manage Seats
                </a>

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

    setSelectedLayoutValue(
        SEATING_LAYOUT_NAME_TO_VALUE[event.seatingLayoutType] ||
        SEATING_LAYOUT_TYPE.STRAIGHT_ROWS
    );


    eventFormTitle.textContent =
        "Edit Event";

    saveEventButton.textContent =
        "Update Event";

    cancelEditButton.hidden =
        false;


    goToStep(1);

    eventWizardCard.scrollIntoView({
        behavior: "smooth",
        block: "start"
    });
}


function resetEventForm() {

    eventForm.reset();

    eventIdInput.value = "";

    setSelectedLayoutValue(SEATING_LAYOUT_TYPE.STRAIGHT_ROWS);

    eventFormTitle.textContent =
        "Add New Event";

    saveEventButton.textContent =
        "Save Event";

    cancelEditButton.hidden =
        true;

    goToStep(1);
    hideWizardFeedback();
}


/* ---------------- Wizard step navigation ---------------- */

function goToStep(stepNumber) {

    currentStep = stepNumber;

    stepPanels.forEach((panel) => {
        panel.hidden = Number(panel.dataset.step) !== stepNumber;
    });

    stepIndicators.forEach((indicator) => {
        const indicatorStep = Number(indicator.dataset.stepIndicator);

        indicator.classList.toggle("is-active", indicatorStep === stepNumber);
        indicator.classList.toggle("is-complete", indicatorStep < stepNumber);
    });

    wizardBackButton.hidden = stepNumber === 1;
    wizardNextButton.hidden = stepNumber === TOTAL_STEPS;
    saveEventButton.hidden = stepNumber !== TOTAL_STEPS;

    if (stepNumber === TOTAL_STEPS) {
        populateReviewStep();
    }

    hideWizardFeedback();
}


function validateStep(stepNumber) {

    const panel = stepPanels.find(
        (item) => Number(item.dataset.step) === stepNumber
    );

    if (!panel) {
        return true;
    }

    const requiredFields = Array.from(
        panel.querySelectorAll("input[required], select[required], textarea[required]")
    );

    for (const field of requiredFields) {
        if (!field.checkValidity()) {
            field.reportValidity();
            return false;
        }
    }

    if (stepNumber === 1) {
        if (
            startDateTimeInput.value &&
            endDateTimeInput.value &&
            new Date(endDateTimeInput.value) <= new Date(startDateTimeInput.value)
        ) {
            showWizardFeedback(
                "End date and time must be after start date and time.",
                "error"
            );
            return false;
        }
    }

    if (stepNumber === 2) {
        const venue = venues.find(
            (item) => item.id === Number(venueIdInput.value)
        );

        const capacity = Number(eventCapacityInput.value);

        if (venue && capacity > venue.totalCapacity) {
            showWizardFeedback(
                `Capacity cannot exceed the venue's total capacity (${venue.totalCapacity}).`,
                "error"
            );
            return false;
        }
    }

    return true;
}


wizardNextButton.addEventListener("click", () => {
    if (!validateStep(currentStep)) {
        return;
    }

    if (currentStep < TOTAL_STEPS) {
        goToStep(currentStep + 1);
    }
});


wizardBackButton.addEventListener("click", () => {
    if (currentStep > 1) {
        goToStep(currentStep - 1);
    }
});


function getSelectedLayoutValue() {
    const checked = layoutInputs.find((input) => input.checked);
    return checked ? Number(checked.value) : SEATING_LAYOUT_TYPE.STRAIGHT_ROWS;
}


function setSelectedLayoutValue(value) {
    layoutInputs.forEach((input) => {
        input.checked = Number(input.value) === Number(value);
    });

    updateLayoutOptionSelectionStyles();
}


function updateLayoutOptionSelectionStyles() {
    document.querySelectorAll(".layout-option").forEach((label) => {
        label.classList.toggle(
            "is-selected",
            label.querySelector("input").checked
        );
    });
}


layoutInputs.forEach((input) => {
    input.addEventListener("change", updateLayoutOptionSelectionStyles);
});

updateLayoutOptionSelectionStyles();


function layoutLabelFromName(seatingLayoutTypeName) {
    const value = SEATING_LAYOUT_NAME_TO_VALUE[seatingLayoutTypeName];
    return SEATING_LAYOUT_VALUE_TO_LABEL[value] || "Hall / Straight";
}


function populateReviewStep() {

    document.getElementById("reviewName").textContent =
        eventNameInput.value.trim() || "-";

    document.getElementById("reviewCategory").textContent =
        getCategoryName(Number(categoryIdInput.value));

    document.getElementById("reviewStart").textContent =
        formatDateTime(startDateTimeInput.value);

    document.getElementById("reviewEnd").textContent =
        formatDateTime(endDateTimeInput.value);

    document.getElementById("reviewVenue").textContent =
        getVenueName(Number(venueIdInput.value));

    document.getElementById("reviewCapacity").textContent =
        eventCapacityInput.value || "-";

    document.getElementById("reviewLayout").textContent =
        SEATING_LAYOUT_VALUE_TO_LABEL[getSelectedLayoutValue()];

    document.getElementById("reviewTicketPrice").textContent =
        formatMoney(ticketPriceInput.value);

    document.getElementById("reviewParkingFee").textContent =
        formatMoney(parkingFeeInput.value);

    document.getElementById("reviewDescription").textContent =
        eventDescriptionInput.value.trim() || "-";
}


async function saveEvent(event) {

    event.preventDefault();

    if (!validateStep(TOTAL_STEPS)) {
        return;
    }


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
            Number(parkingFeeInput.value),

        seatingLayoutType:
            getSelectedLayoutValue()
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

        showWizardFeedback(
            "Please enter valid event details.",
            "error"
        );

        return;
    }


    if (
        new Date(eventData.endDateTime) <=
        new Date(eventData.startDateTime)
    ) {

        showWizardFeedback(
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

        hideWizardFeedback();


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

        showWizardFeedback(
            error?.data?.message ||
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
            error?.data?.message ||
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


function showWizardFeedback(message, type) {
    wizardFeedback.textContent = message;
    wizardFeedback.className = `feedback feedback-${type}`;
    wizardFeedback.hidden = false;
}


function hideWizardFeedback() {
    wizardFeedback.hidden = true;
    wizardFeedback.textContent = "";
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
