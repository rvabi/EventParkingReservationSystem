import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

import { renderSeatMap, SEAT_STATUS } from "./seat-map.js";


const eventNameHeading = document.getElementById("eventNameHeading");
const eventMetaLine = document.getElementById("eventMetaLine");
const backToEventLink = document.getElementById("backToEventLink");
const seatSelectionLayout = document.getElementById("seatSelectionLayout");
const seatMapContainer = document.getElementById("seatMapContainer");
const refreshSeatsButton = document.getElementById("refreshSeatsButton");
const selectedSeatsList = document.getElementById("selectedSeatsList");
const selectedSeatsTotal = document.getElementById("selectedSeatsTotal");
const continueButton = document.getElementById("continueButton");
const validationFeedback = document.getElementById("validationFeedback");

/*
 * No persisted layout-type field exists on Event/Venue/Seat yet (verified
 * against current entities/DTOs), so the customer view always renders the
 * conventional Square layout as a safe documented default. See the Seat
 * phase report for what a persisted field would need to look like.
 */
const LAYOUT_MODE = "square";

let eventId = null;
let seatMap = null;
let selectedSeatIds = new Set();
let selectedSeats = new Map();


document.addEventListener("DOMContentLoaded", async () => {
    renderNavbar();
    await initializePage();
});


async function initializePage() {
    const parameters = new URLSearchParams(window.location.search);
    eventId = Number(parameters.get("id"));

    if (!eventId || eventId <= 0) {
        showFeedback("Invalid event selected.", "error");
        return;
    }

    backToEventLink.href = `./event-details.html?id=${eventId}`;

    await loadEventAndSeats();
}


async function loadEventAndSeats() {
    try {
        hideFeedback();
        seatSelectionLayout.hidden = true;
        eventMetaLine.textContent = "Loading event information...";

        const eventItem = await api.get(`/api/Events/${eventId}`);
        const venue = await api.get(`/api/Venues/${eventItem.venueId}`);

        eventNameHeading.textContent = eventItem.name;

        eventMetaLine.textContent =
            `${venue.name} | ${formatDate(eventItem.startDateTime)} | ` +
            `${formatTime(eventItem.startDateTime)} - ${formatTime(eventItem.endDateTime)}`;

        await loadSeatMap();
    } catch (error) {
        if (error?.status === 404) {
            showFeedback("Event not found.", "error");
            return;
        }

        showFeedback(
            error.message || "Unable to load event information.",
            "error"
        );
    }
}


async function loadSeatMap() {
    try {
        seatMap = await api.get(`/api/events/${eventId}/seats`);

        selectedSeatIds = new Set();
        selectedSeats = new Map();

        seatSelectionLayout.hidden = false;

        if (!seatMap.seats || seatMap.seats.length === 0) {
            renderSeatMap(seatMapContainer, {
                seats: [],
                layout: LAYOUT_MODE,
                mode: "customer"
            });

            showFeedback(
                "Seats haven't been made available for this event yet.",
                "info"
            );

            updateSummary();
            return;
        }

        hideFeedback();
        renderSeats();
        updateSummary();
    } catch (error) {
        if (error?.status === 404) {
            showFeedback("Event not found.", "error");
            return;
        }

        showFeedback(
            error.message || "Unable to load the seat map.",
            "error"
        );
    }
}


function renderSeats() {
    renderSeatMap(seatMapContainer, {
        seats: seatMap.seats,
        layout: LAYOUT_MODE,
        mode: "customer",
        selectedSeatIds,
        onSeatClick: handleSeatClick
    });
}


function handleSeatClick(seat) {
    if (seat.status !== SEAT_STATUS.AVAILABLE) {
        return;
    }

    if (selectedSeatIds.has(seat.id)) {
        selectedSeatIds.delete(seat.id);
        selectedSeats.delete(seat.id);
    } else {
        selectedSeatIds.add(seat.id);
        selectedSeats.set(seat.id, seat);
    }

    hideValidationFeedback();
    renderSeats();
    updateSummary();
}


function updateSummary() {
    if (selectedSeats.size === 0) {
        selectedSeatsList.innerHTML =
            `<p class="selected-seats-empty">No seats selected yet.</p>`;

        selectedSeatsTotal.textContent = formatMoney(0);
        continueButton.disabled = true;
        return;
    }

    const selected = Array.from(selectedSeats.values()).sort(
        (a, b) => a.seatNumber.localeCompare(
            b.seatNumber,
            undefined,
            { numeric: true }
        )
    );

    let total = 0;

    selectedSeatsList.innerHTML = selected.map((seat) => {
        total += Number(seat.price) || 0;

        return `
            <div class="selected-seat-row">
                <span>${escapeHtml(seat.seatNumber)}</span>
                <span>${formatMoney(seat.price)}</span>
            </div>
        `;
    }).join("");

    selectedSeatsTotal.textContent = formatMoney(total);
    continueButton.disabled = false;
}


async function handleContinue() {
    if (selectedSeats.size === 0) {
        return;
    }

    try {
        setButtonLoading(continueButton, true, "Validating...");
        hideValidationFeedback();

        const seatIds = Array.from(selectedSeatIds);

        await api.post(`/api/events/${eventId}/seats/validate`, seatIds);

        showValidationFeedback(
            "Your seat selection is valid. Booking checkout isn't implemented " +
            "yet - that's the next required integration.",
            "success"
        );
    } catch (error) {
        showValidationFeedback(
            error?.data?.message ||
            error.message ||
            "Selection could not be validated. Please refresh and try again.",
            "error"
        );
    } finally {
        setButtonLoading(continueButton, false);
    }
}


function showValidationFeedback(message, type) {
    validationFeedback.textContent = message;
    validationFeedback.className = `feedback feedback-${type}`;
    validationFeedback.hidden = false;
}


function hideValidationFeedback() {
    validationFeedback.hidden = true;
    validationFeedback.textContent = "";
}


function formatDate(value) {
    if (!value) {
        return "Not available";
    }

    return new Date(value).toLocaleDateString();
}


function formatTime(value) {
    if (!value) {
        return "";
    }

    return new Date(value).toLocaleTimeString(
        [],
        { hour: "2-digit", minute: "2-digit" }
    );
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


refreshSeatsButton.addEventListener("click", loadSeatMap);
continueButton.addEventListener("click", handleContinue);
