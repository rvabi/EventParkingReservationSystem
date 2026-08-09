import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";


const availabilityForm =
    document.getElementById("availabilityForm");

const startDateTimeInput =
    document.getElementById("startDateTime");

const endDateTimeInput =
    document.getElementById("endDateTime");

const checkAvailabilityButton =
    document.getElementById("checkAvailabilityButton");

const clearAvailabilityButton =
    document.getElementById("clearAvailabilityButton");

const availableVenueList =
    document.getElementById("availableVenueList");


document.addEventListener(
    "DOMContentLoaded",
    () => {
        renderNavbar();
    }
);


async function checkAvailability(event) {

    event.preventDefault();

    const startDateTime =
        startDateTimeInput.value;

    const endDateTime =
        endDateTimeInput.value;


    if (!startDateTime || !endDateTime) {

        showFeedback(
            "Please select both start and end date/time.",
            "error"
        );

        return;
    }


    if (
        new Date(endDateTime) <=
        new Date(startDateTime)
    ) {

        showFeedback(
            "End date and time must be after start date and time.",
            "error"
        );

        return;
    }


    try {

        setButtonLoading(
            checkAvailabilityButton,
            true,
            "Checking..."
        );

        hideFeedback();

        availableVenueList.innerHTML = "";


        const query =
            new URLSearchParams({
                startDateTime:
                    startDateTime,

                endDateTime:
                    endDateTime
            });


        const venues =
            await api.get(
                `/api/Venues/available?${query.toString()}`
            );


        renderAvailableVenues(venues);

    } catch (error) {

        availableVenueList.innerHTML = "";

        showFeedback(
            error.message ||
            "Unable to check venue availability.",
            "error"
        );

    } finally {

        setButtonLoading(
            checkAvailabilityButton,
            false
        );
    }
}


function renderAvailableVenues(venues) {

    availableVenueList.innerHTML = "";


    if (!venues || venues.length === 0) {

        showFeedback(
            "No venues are available for the selected time.",
            "info"
        );

        return;
    }


    hideFeedback();


    venues.forEach((venue) => {

        const card =
            document.createElement("article");

        card.className =
            "service-card";


        card.innerHTML = `

            <div class="service-number">
                #${venue.id}
            </div>

            <div class="service-icon">
                V
            </div>

            <h3>
                ${escapeHtml(venue.name)}
            </h3>

            <p>
                <strong>Address:</strong>
                ${escapeHtml(venue.address)}
            </p>

            <p>
                <strong>Total Capacity:</strong>
                ${venue.totalCapacity}
            </p>

            <span class="status-badge status-active">
                Available
            </span>
        `;


        availableVenueList.appendChild(card);
    });
}


function clearAvailability() {

    availabilityForm.reset();

    availableVenueList.innerHTML = "";

    hideFeedback();
}


function escapeHtml(value) {

    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}


availabilityForm.addEventListener(
    "submit",
    checkAvailability
);


clearAvailabilityButton.addEventListener(
    "click",
    clearAvailability
);