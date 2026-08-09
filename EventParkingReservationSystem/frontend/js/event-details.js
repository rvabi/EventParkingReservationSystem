import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback
} from "./ui.js";


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


        const [venue, category] =
            await Promise.all([

                api.get(
                    `/api/Venues/${eventItem.venueId}`
                ),

                api.get(
                    `/api/Categories/${eventItem.eventCategoryId}`
                )

            ]);


        renderEventDetails(
            eventItem,
            venue,
            category
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


function renderEventDetails(
    eventItem,
    venue,
    category
) {

    eventDetails.innerHTML = `
        <article class="service-card">

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
                ${escapeHtml(venue.name)}
            </p>

            <p>
                <strong>Address:</strong>
                ${escapeHtml(
                    venue.address || "-"
                )}
            </p>

            <p>
                <strong>Category:</strong>
                ${escapeHtml(category.name)}
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

            <p>
                <strong>Ticket Price:</strong>
                Rs.
                ${formatMoney(
                    eventItem.ticketPrice
                )}
            </p>

            <p>
                <strong>Parking Fee:</strong>
                Rs.
                ${formatMoney(
                    eventItem.parkingFee
                )}
            </p>

            <p>
                <strong>Event Capacity:</strong>
                ${eventItem.capacity}
            </p>

            <p>
                <strong>Venue Capacity:</strong>
                ${venue.totalCapacity}
            </p>

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