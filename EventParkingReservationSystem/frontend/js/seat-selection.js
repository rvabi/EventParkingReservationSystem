import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

import { renderSeatMap, SEAT_STATUS, layoutModeFromSeatingLayoutType, groupSeatsByRow } from "./seat-map.js";
import { isAuthenticated, getCustomerRole } from "./auth.js";


const eventNameHeading = document.getElementById("eventNameHeading");
const eventMetaLine = document.getElementById("eventMetaLine");
const backToEventLink = document.getElementById("backToEventLink");
const authGuardPanel = document.getElementById("authGuardPanel");
const authGuardMessage = document.getElementById("authGuardMessage");
const authGuardLink = document.getElementById("authGuardLink");
const seatSelectionLayout = document.getElementById("seatSelectionLayout");
const seatMapView = document.getElementById("seatMapView");
const seatMapContainer = document.getElementById("seatMapContainer");
const refreshSeatsButton = document.getElementById("refreshSeatsButton");
const selectionView = document.getElementById("selectionView");
const selectedSeatsList = document.getElementById("selectedSeatsList");
const selectedSeatsTotal = document.getElementById("selectedSeatsTotal");
const continueButtonRow = document.getElementById("continueButtonRow");
const continueButton = document.getElementById("continueButton");
const validationFeedback = document.getElementById("validationFeedback");
const seatsAvailableStrip = document.getElementById("seatsAvailableStrip");

const seatChooser = document.getElementById("seatChooser");
const chooserRingSelect = document.getElementById("chooserRingSelect");
const chooserSeatSelect = document.getElementById("chooserSeatSelect");
const chooserSeatPrice = document.getElementById("chooserSeatPrice");
const chooserAddSeatButton = document.getElementById("chooserAddSeatButton");

const eventOverviewPanel = document.getElementById("eventOverviewPanel");
const overviewEventName = document.getElementById("overviewEventName");
const overviewEventMeta = document.getElementById("overviewEventMeta");
const overviewVenue = document.getElementById("overviewVenue");
const overviewCategory = document.getElementById("overviewCategory");
const overviewDescription = document.getElementById("overviewDescription");
const overviewTicketPrice = document.getElementById("overviewTicketPrice");
const overviewSelectedSeats = document.getElementById("overviewSelectedSeats");
const overviewSeatTotal = document.getElementById("overviewSeatTotal");

const parkingStep = document.getElementById("parkingStep");
const parkingStepHeading = document.getElementById("parkingStepHeading");
const parkingOptions = document.getElementById("parkingOptions");
const parkingFeedback = document.getElementById("parkingFeedback");
const bookingFeedback = document.getElementById("bookingFeedback");
const createBookingButton = document.getElementById("createBookingButton");

/*
 * Layout is read automatically from the persisted Event.SeatingLayoutType
 * ("StraightRows" | "CircularArena", see EventDto) once the event loads -
 * the customer never chooses or sees a layout selector; see
 * layoutModeFromSeatingLayoutType in seat-map.js for the shared mapping
 * used by both this page and the admin Manage Seats page.
 */
let layoutMode = "square";

let eventId = null;
let eventItem = null;
let eventVenue = null;
let eventCategory = null;
let seatMap = null;
let selectedSeatIds = new Set();
let selectedSeats = new Map();

let parkingSlots = [];
let selectedParkingSlotId = null;
let seatsAreLocked = false;


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

    if (!isAuthenticated()) {
        showAuthGuard(
            "Please log in with a customer account to select seats and book.",
            "login.html"
        );
        return;
    }

    if (getCustomerRole() !== "Customer") {
        showAuthGuard(
            "Seat booking is only available to customer accounts. " +
            "Administrators should use the admin dashboard.",
            "manage-events.html"
        );
        return;
    }

    await loadEventAndSeats();
}


function showAuthGuard(message, linkHref) {
    authGuardMessage.textContent = message;
    authGuardLink.href = linkHref;
    authGuardPanel.hidden = false;
}


async function loadEventAndSeats() {
    try {
        hideFeedback();
        seatSelectionLayout.hidden = true;
        eventMetaLine.textContent = "Loading event information...";

        eventItem = await api.get(`/api/Events/${eventId}`);

        layoutMode = layoutModeFromSeatingLayoutType(eventItem.seatingLayoutType);

        [eventVenue, eventCategory] = await Promise.all([
            api.get(`/api/Venues/${eventItem.venueId}`),
            api.get(`/api/Categories/${eventItem.eventCategoryId}`)
        ]);

        eventNameHeading.textContent = eventItem.name;

        eventMetaLine.textContent =
            `${eventVenue.name} | ${formatDate(eventItem.startDateTime)} | ` +
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
        seatsAreLocked = false;
        resetParkingStep();

        seatSelectionLayout.hidden = false;

        if (!seatMap.seats || seatMap.seats.length === 0) {
            renderSeatMap(seatMapContainer, {
                seats: [],
                layout: layoutMode,
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
        populateRingOptions();
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
        layout: layoutMode,
        mode: "customer",
        selectedSeatIds,
        onSeatClick: seatsAreLocked ? null : handleSeatClick
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

    if (layoutMode === "circular") {
        populateSeatOptions();
    }
}


/*
 * Ring/Seat chooser (CircularArena only). This reuses the exact same
 * selectedSeatIds/selectedSeats state and handleSeatClick() toggle as
 * clicking a dot directly - it is a second way to drive the same
 * selection, never a second parallel selection state. Only Available
 * seats not already selected are offered, so "Add Seat" always adds
 * (never needs to toggle off) and the seat disappears from the dropdown
 * the moment it is selected, exactly as required.
 */
function populateRingOptions() {
    if (layoutMode !== "circular" || !seatMap?.seats?.length) {
        seatChooser.hidden = true;
        return;
    }

    const rows = groupSeatsByRow(seatMap.seats);

    if (!rows.length) {
        seatChooser.hidden = true;
        return;
    }

    seatChooser.hidden = false;

    const previousRing = chooserRingSelect.value;

    chooserRingSelect.innerHTML = rows.map((row) =>
        `<option value="${escapeHtml(row.rowLabel)}">Ring ${escapeHtml(row.rowLabel)}</option>`
    ).join("");

    if (previousRing && rows.some((row) => row.rowLabel === previousRing)) {
        chooserRingSelect.value = previousRing;
    }

    populateSeatOptions();
}


function populateSeatOptions() {
    if (seatChooser.hidden) {
        return;
    }

    const ring = chooserRingSelect.value;

    const availableSeats = seatMap.seats
        .filter((seat) =>
            seat.rowLabel === ring &&
            seat.status === SEAT_STATUS.AVAILABLE &&
            !selectedSeatIds.has(seat.id))
        .sort((a, b) => (a.columnNumber || 0) - (b.columnNumber || 0));

    if (!availableSeats.length) {
        chooserSeatSelect.innerHTML = `<option value="">No available seats in this ring</option>`;
        chooserSeatSelect.disabled = true;
        chooserAddSeatButton.disabled = true;
        chooserSeatPrice.textContent = formatMoney(0);
        return;
    }

    chooserSeatSelect.disabled = false;
    chooserAddSeatButton.disabled = false;

    chooserSeatSelect.innerHTML = availableSeats.map((seat) =>
        `<option value="${seat.id}">${escapeHtml(seat.seatNumber)}</option>`
    ).join("");

    updateChooserPrice();
}


function updateChooserPrice() {
    const seatId = Number(chooserSeatSelect.value);
    const seat = seatMap.seats.find((item) => item.id === seatId);

    chooserSeatPrice.textContent = formatMoney(seat ? seat.price : 0);
}


chooserRingSelect.addEventListener("change", populateSeatOptions);
chooserSeatSelect.addEventListener("change", updateChooserPrice);

chooserAddSeatButton.addEventListener("click", () => {
    const seatId = Number(chooserSeatSelect.value);
    const seat = seatMap.seats.find((item) => item.id === seatId);

    if (!seat) {
        return;
    }

    handleSeatClick(seat);
});


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
                <button
                    type="button"
                    class="selected-seat-remove"
                    data-seat-id="${seat.id}"
                    aria-label="Remove seat ${escapeHtml(seat.seatNumber)}">
                    &times;
                </button>
            </div>
        `;
    }).join("");

    selectedSeatsList.querySelectorAll(".selected-seat-remove").forEach((button) => {
        button.addEventListener("click", () => {
            const seat = selectedSeats.get(Number(button.dataset.seatId));

            if (seat) {
                handleSeatClick(seat);
            }
        });
    });

    selectedSeatsTotal.textContent = formatMoney(total);

    if (!seatsAreLocked) {
        continueButton.disabled = false;
    }
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

        seatsAreLocked = true;
        continueButtonRow.hidden = true;
        seatsAvailableStrip.hidden = false;

        showParkingFocusedView();

        await loadParkingStep();

        moveFocusToParkingStep();
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


function moveFocusToParkingStep() {
    const reducedMotion =
        window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    parkingStep.scrollIntoView({
        behavior: reducedMotion ? "auto" : "smooth",
        block: "start"
    });

    parkingStepHeading.focus({ preventScroll: true });
}


/* ---------------- Parking (optional, attached at booking creation) ---------------- */

function resetParkingStep() {
    parkingStep.hidden = true;
    parkingOptions.innerHTML = "";
    parkingSlots = [];
    selectedParkingSlotId = null;
    continueButtonRow.hidden = false;
    seatsAvailableStrip.hidden = true;
    hideValidationFeedback();
    hideParkingFeedback();
    hideBookingFeedback();

    seatMapView.hidden = false;
    eventOverviewPanel.hidden = true;
    selectionView.hidden = false;
}


/*
 * Switches the left column from the seat map (with its SeatStatus legend)
 * to a read-only Event Overview once seats are validated - the legend is
 * only meaningful while actively picking seats. The right column drops
 * the "Your Selection" block at the same time since the same seat/total
 * data now lives in the overview instead, keeping the Parking step from
 * competing with duplicate information.
 */
function showParkingFocusedView() {
    seatMapView.hidden = true;
    selectionView.hidden = true;

    renderEventOverview();
    eventOverviewPanel.hidden = false;
}


function renderEventOverview() {
    if (!eventItem) {
        return;
    }

    overviewEventName.textContent = eventItem.name;

    overviewEventMeta.textContent =
        `${formatDate(eventItem.startDateTime)} · ${formatTime(eventItem.startDateTime)}`;

    overviewVenue.textContent = eventVenue ? eventVenue.name : "";
    overviewCategory.textContent = eventCategory ? eventCategory.name : "";

    overviewDescription.textContent = eventItem.description || "";
    overviewDescription.hidden = !eventItem.description;

    overviewTicketPrice.textContent =
        `Ticket price: ${formatMoney(eventItem.ticketPrice)}`;

    const selected = Array.from(selectedSeats.values()).sort(
        (a, b) => a.seatNumber.localeCompare(
            b.seatNumber,
            undefined,
            { numeric: true }
        )
    );

    let total = 0;

    overviewSelectedSeats.innerHTML = selected.map((seat) => {
        total += Number(seat.price) || 0;

        return `
            <div class="selected-seat-row">
                <span>${escapeHtml(seat.seatNumber)}</span>
                <span>${formatMoney(seat.price)}</span>
            </div>
        `;
    }).join("");

    overviewSeatTotal.textContent = formatMoney(total);
}


async function loadParkingStep() {
    parkingStep.hidden = false;
    selectedParkingSlotId = null;
    hideParkingFeedback();

    try {
        parkingSlots = await api.get(`/api/events/${eventId}/parking-slots`);

        if (!parkingSlots.length) {
            showParkingFeedback(
                "No parking slots are currently available. " +
                "You can continue without parking.",
                "info"
            );
        }
    } catch (error) {
        parkingSlots = [];
        showParkingFeedback(
            error?.data?.message ||
            error.message ||
            "Unable to load parking slots. You can still book without parking.",
            "error"
        );
    }

    renderParkingOptions();
}


function renderParkingOptions() {
    const noParkingCard = `
        <label class="parking-option is-selected" data-slot-id="">
            <input type="radio" name="parkingSlot" value="" checked>
            <span class="parking-option-title">No Parking</span>
            <span class="parking-option-detail">Skip parking for this booking.</span>
        </label>
    `;

    if (!parkingSlots.length) {
        parkingOptions.innerHTML = noParkingCard;
        attachParkingOptionEvents();
        return;
    }

    const slotCards = parkingSlots.map((slot) => {
        const isAvailable = slot.status === "Available";

        return `
            <label class="parking-option${isAvailable ? "" : " is-disabled"}" data-slot-id="${slot.id}">
                <input type="radio" name="parkingSlot" value="${slot.id}" ${isAvailable ? "" : "disabled"}>
                <span class="parking-option-title">
                    Slot ${escapeHtml(slot.slotNumber)}
                    ${slot.zone ? `&middot; ${escapeHtml(slot.zone)}` : ""}
                </span>
                <span class="parking-option-detail">
                    ${isAvailable ? formatMoney(slot.effectiveFee) : slot.status}
                </span>
            </label>
        `;
    }).join("");

    parkingOptions.innerHTML = noParkingCard + slotCards;
    attachParkingOptionEvents();
}


function attachParkingOptionEvents() {
    parkingOptions.querySelectorAll(".parking-option input").forEach((input) => {
        if (input.disabled) {
            return;
        }

        input.addEventListener("change", () => {
            selectedParkingSlotId = input.value ? Number(input.value) : null;
            updateParkingOptionSelectionStyles();
        });
    });

    updateParkingOptionSelectionStyles();
}


function updateParkingOptionSelectionStyles() {
    parkingOptions.querySelectorAll(".parking-option").forEach((label) => {
        label.classList.toggle("is-selected", label.querySelector("input").checked);
    });
}


async function handleCreateBooking() {
    try {
        setButtonLoading(createBookingButton, true, "Creating booking...");
        hideBookingFeedback();

        const booking = await api.post("/api/bookings", {
            eventId,
            seatIds: Array.from(selectedSeatIds),
            parkingSlotId: selectedParkingSlotId
        });

        window.location.assign(`./booking-summary.html?id=${booking.id}`);
    } catch (error) {
        if (error?.status === 401) {
            showAuthGuard(
                "Your session has expired. Please log in again to continue.",
                "login.html"
            );
            parkingStep.hidden = true;
            return;
        }

        showBookingFeedback(
            error?.data?.message ||
            error.message ||
            "Unable to create your booking. Please refresh and try again.",
            "error"
        );
    } finally {
        setButtonLoading(createBookingButton, false);
    }
}


function showParkingFeedback(message, type) {
    parkingFeedback.textContent = message;
    parkingFeedback.className = `feedback feedback-${type}`;
    parkingFeedback.hidden = false;
}


function hideParkingFeedback() {
    parkingFeedback.hidden = true;
    parkingFeedback.textContent = "";
}


function showBookingFeedback(message, type) {
    bookingFeedback.textContent = message;
    bookingFeedback.className = `feedback feedback-${type}`;
    bookingFeedback.hidden = false;
}


function hideBookingFeedback() {
    bookingFeedback.hidden = true;
    bookingFeedback.textContent = "";
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
createBookingButton.addEventListener("click", handleCreateBooking);
