import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

import {
    renderSeatMap,
    SEAT_STATUS,
    groupSeatsByRow,
    layoutModeFromSeatingLayoutType
} from "./seat-map.js";

import { isAuthenticated, getCustomerRole } from "./auth.js";


const authGuardPanel = document.getElementById("authGuardPanel");
const authGuardMessage = document.getElementById("authGuardMessage");
const authGuardLink = document.getElementById("authGuardLink");

const seatAdminBody = document.getElementById("seatAdminBody");

const eventSelect = document.getElementById("eventSelect");
const seatManagementPanel = document.getElementById("seatManagementPanel");
const layoutNote = document.getElementById("layoutNote");

const statCapacity = document.getElementById("statCapacity");
const statGenerated = document.getElementById("statGenerated");
const statTicketPrice = document.getElementById("statTicketPrice");

const generatePanel = document.getElementById("generatePanel");
const rowCountLabel = document.getElementById("rowCountLabel");
const rowCountInput = document.getElementById("rowCountInput");
const autoDistributeButton = document.getElementById("autoDistributeButton");
const rowDefinitions = document.getElementById("rowDefinitions");
const addRowButton = document.getElementById("addRowButton");
const rowTotalSummary = document.getElementById("rowTotalSummary");
const generateFeedback = document.getElementById("generateFeedback");
const generateButton = document.getElementById("generateButton");

const existingMapPanel = document.getElementById("existingMapPanel");
const seatMapContainer = document.getElementById("seatMapContainer");
const seatActionFeedback = document.getElementById("seatActionFeedback");
const rowPricingHeading = document.getElementById("rowPricingHeading");
const rowPriceEditor = document.getElementById("rowPriceEditor");


let events = [];
let currentEventId = null;
let currentEvent = null;
let seatMap = null;


document.addEventListener("DOMContentLoaded", async () => {
    renderNavbar();

    if (!isAuthenticated()) {
        showAuthGuard(
            "Please log in with an administrator account to manage seats.",
            "login.html"
        );
        return;
    }

    if (getCustomerRole() !== "Administrator") {
        showAuthGuard(
            "Seat management is only available to administrator accounts.",
            "login.html"
        );
        return;
    }

    seatAdminBody.hidden = false;

    await initializePage();
});


function showAuthGuard(message, linkHref) {
    authGuardMessage.textContent = message;
    authGuardLink.href = linkHref;
    authGuardPanel.hidden = false;
}


/* ---------------- Row / Ring terminology ---------------- */

function isCircularArena() {
    return Boolean(currentEvent) && currentEvent.seatingLayoutType === "CircularArena";
}


function rowUnitLabel() {
    return isCircularArena() ? "Ring" : "Row";
}


function currentLayoutMode() {
    return layoutModeFromSeatingLayoutType(
        currentEvent ? currentEvent.seatingLayoutType : "StraightRows"
    );
}


function updateLayoutAwareLabels() {
    const unit = rowUnitLabel();

    rowCountLabel.textContent = `Number of ${unit}s`;
    addRowButton.textContent = `+ Add ${unit}`;
    rowPricingHeading.textContent = `${unit} Pricing`;

    layoutNote.textContent = isCircularArena()
        ? "Layout: Ground / Full Round (360° circular arena)"
        : "Layout: Hall / Straight (rows facing a stage)";
}


async function initializePage() {
    try {
        hideFeedback();

        events = await api.get("/api/Events");

        eventSelect.innerHTML = `<option value="">Select an event</option>`;

        events.forEach((eventItem) => {
            const option = document.createElement("option");
            option.value = eventItem.id;
            option.textContent = `${eventItem.name} (Capacity: ${eventItem.capacity})`;
            eventSelect.appendChild(option);
        });

        const requestedId =
            Number(new URLSearchParams(window.location.search).get("id"));

        if (requestedId && events.some((eventItem) => eventItem.id === requestedId)) {
            eventSelect.value = String(requestedId);
            currentEventId = requestedId;
            currentEvent = events.find((eventItem) => eventItem.id === requestedId);
            await loadSeatMapForEvent();
        }
    } catch (error) {
        showFeedback(
            error.message || "Unable to load events.",
            "error"
        );
    }
}


eventSelect.addEventListener("change", async () => {
    const value = eventSelect.value;

    if (!value) {
        seatManagementPanel.hidden = true;
        currentEventId = null;
        currentEvent = null;
        seatMap = null;
        return;
    }

    currentEventId = Number(value);
    currentEvent = events.find((eventItem) => eventItem.id === currentEventId);
    await loadSeatMapForEvent();
});


async function loadSeatMapForEvent() {
    try {
        hideFeedback();
        seatManagementPanel.hidden = true;

        seatMap = await api.get(`/api/events/${currentEventId}/seats`);

        seatManagementPanel.hidden = false;
        updateLayoutAwareLabels();

        statCapacity.textContent = seatMap.capacity;
        statGenerated.textContent = seatMap.totalSeats;
        statTicketPrice.textContent = formatMoney(seatMap.ticketPrice);

        if (!seatMap.seats || seatMap.seats.length === 0) {
            generatePanel.hidden = false;
            existingMapPanel.hidden = true;
            resetRowDefinitions();
        } else {
            generatePanel.hidden = true;
            existingMapPanel.hidden = false;
            renderAdminSeatMap();
            renderRowPriceEditor();
        }
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


/* ---------------- Generate Seat Map ---------------- */

function resetRowDefinitions() {
    rowDefinitions.innerHTML = "";
    rowCountInput.value = "1";
    updateRowTotalSummary();
    hideGenerateFeedback();
}


function addRowDefinitionRow(initialSeatCount = 1) {
    const row = document.createElement("div");
    row.className = "row-definition-item";

    row.innerHTML = `
        <div class="form-group">
            <label class="form-label">Seat Count</label>
            <input type="number" min="1" class="form-control row-seat-count" value="${initialSeatCount}">
        </div>
        <div class="form-group">
            <label class="form-label">${rowUnitLabel()} Price (optional)</label>
            <input type="number" min="0" step="0.01" class="form-control row-price" placeholder="Default ticket price">
        </div>
        <button type="button" class="btn btn-secondary btn-small remove-row-button">Remove</button>
    `;

    row.querySelector(".remove-row-button").addEventListener("click", () => {
        row.remove();
        updateRowTotalSummary();
    });

    row.querySelector(".row-seat-count").addEventListener(
        "input",
        updateRowTotalSummary
    );

    rowDefinitions.appendChild(row);
    updateRowTotalSummary();
}


function updateRowTotalSummary() {
    const counts = Array.from(
        rowDefinitions.querySelectorAll(".row-seat-count")
    ).map((input) => Number(input.value) || 0);

    const total = counts.reduce((sum, n) => sum + n, 0);
    const capacity = seatMap ? seatMap.capacity : 0;
    const diff = capacity - total;
    const matches = diff === 0 && counts.length > 0;

    rowTotalSummary.textContent =
        matches
            ? `Total Seats: ${total} / ${capacity} ✓`
            : diff > 0
                ? `Total Seats: ${total} / ${capacity} — ${diff} seat(s) remaining.`
                : `Total Seats: ${total} / ${capacity} — ${Math.abs(diff)} seat(s) over capacity.`;

    rowTotalSummary.classList.toggle("row-total-summary-error", !matches);
    rowTotalSummary.classList.toggle("row-total-summary-ok", matches);

    generateButton.disabled = !matches;
}


/*
 * Evenly distributes `capacity` seats across `rowCount` rows/rings, giving
 * the first (capacity % rowCount) rows one extra seat so the total always
 * equals capacity exactly. Example: capacity=103, rowCount=5 -> 21,21,21,20,20.
 */
function distributeSeatsAcrossRows(capacity, rowCount) {
    if (rowCount <= 0 || capacity <= 0) {
        return [];
    }

    const base = Math.floor(capacity / rowCount);
    const remainder = capacity % rowCount;

    return Array.from(
        { length: rowCount },
        (_, index) => base + (index < remainder ? 1 : 0)
    );
}


autoDistributeButton.addEventListener("click", () => {
    const rowCount = Number(rowCountInput.value) || 0;

    if (rowCount < 1) {
        showGenerateFeedback(
            `Enter at least 1 ${rowUnitLabel().toLowerCase()}.`,
            "error"
        );
        return;
    }

    if (!seatMap || seatMap.capacity < rowCount) {
        showGenerateFeedback(
            `Event capacity (${seatMap ? seatMap.capacity : 0}) is too small for ${rowCount} ${rowUnitLabel().toLowerCase()}(s).`,
            "error"
        );
        return;
    }

    const counts = distributeSeatsAcrossRows(seatMap.capacity, rowCount);

    rowDefinitions.innerHTML = "";
    counts.forEach((count) => addRowDefinitionRow(count));

    hideGenerateFeedback();
    updateRowTotalSummary();
});


addRowButton.addEventListener("click", () => addRowDefinitionRow(1));


generateButton.addEventListener("click", async () => {
    const rowItems = Array.from(
        rowDefinitions.querySelectorAll(".row-definition-item")
    );

    const rows = rowItems.map((item) => {
        const seatCount = Number(item.querySelector(".row-seat-count").value) || 0;
        const priceRaw = item.querySelector(".row-price").value;
        const rowPrice = priceRaw === "" ? null : Number(priceRaw);

        return { seatCount, rowPrice };
    });

    if (!rows.length || rows.some((row) => row.seatCount < 1)) {
        showGenerateFeedback(
            `Every ${rowUnitLabel().toLowerCase()} needs at least 1 seat.`,
            "error"
        );
        return;
    }

    const total = rows.reduce((sum, row) => sum + row.seatCount, 0);

    if (total !== seatMap.capacity) {
        showGenerateFeedback(
            `Total seats (${total}) must exactly match event capacity (${seatMap.capacity}).`,
            "error"
        );
        return;
    }

    try {
        setButtonLoading(generateButton, true, "Generating...");
        hideGenerateFeedback();

        await api.post(`/api/events/${currentEventId}/seats`, { rows });

        showFeedback("Seat map generated successfully.", "success");
        await loadSeatMapForEvent();
    } catch (error) {
        showGenerateFeedback(
            error?.data?.message || error.message || "Unable to generate seat map.",
            "error"
        );
    } finally {
        setButtonLoading(generateButton, false);
    }
});


function showGenerateFeedback(message, type) {
    generateFeedback.textContent = message;
    generateFeedback.className = `feedback feedback-${type}`;
    generateFeedback.hidden = false;
}


function hideGenerateFeedback() {
    generateFeedback.hidden = true;
    generateFeedback.textContent = "";
}


/* ---------------- Seat map + seat status (layout is read-only here) ---------------- */

function renderAdminSeatMap() {
    renderSeatMap(seatMapContainer, {
        seats: seatMap.seats,
        layout: currentLayoutMode(),
        mode: "admin",
        onSeatClick: handleAdminSeatClick
    });
}


async function handleAdminSeatClick(seat) {
    if (
        seat.status !== SEAT_STATUS.AVAILABLE &&
        seat.status !== SEAT_STATUS.UNAVAILABLE
    ) {
        return;
    }

    const nextStatus =
        seat.status === SEAT_STATUS.AVAILABLE
            ? SEAT_STATUS.UNAVAILABLE
            : SEAT_STATUS.AVAILABLE;

    const nextStatusLabel =
        nextStatus === SEAT_STATUS.UNAVAILABLE ? "Unavailable" : "Available";

    const confirmed = window.confirm(
        `Mark seat ${seat.seatNumber} as ${nextStatusLabel}?`
    );

    if (!confirmed) {
        return;
    }

    try {
        hideSeatActionFeedback();

        await api.put(`/api/events/${currentEventId}/seats/${seat.id}`, {
            id: seat.id,
            seatNumber: seat.seatNumber,
            rowLabel: seat.rowLabel,
            columnNumber: seat.columnNumber,
            status: nextStatus,
            price: seat.price
        });

        await loadSeatMapForEvent();
    } catch (error) {
        showSeatActionFeedback(
            error?.data?.message || error.message || "Unable to update seat status.",
            "error"
        );
    }
}


function showSeatActionFeedback(message, type) {
    seatActionFeedback.textContent = message;
    seatActionFeedback.className = `feedback feedback-${type}`;
    seatActionFeedback.hidden = false;
}


function hideSeatActionFeedback() {
    seatActionFeedback.hidden = true;
    seatActionFeedback.textContent = "";
}


/* ---------------- Row / Ring Price Editor ---------------- */

function renderRowPriceEditor() {
    const rows = groupSeatsByRow(seatMap.seats);
    const unit = rowUnitLabel();

    rowPriceEditor.innerHTML = "";

    rows.forEach((row) => {
        const effectivePrice =
            row.seats[0] ? Number(row.seats[0].price) : seatMap.ticketPrice;

        const isProtected = row.seats.some(
            (seat) =>
                seat.status === SEAT_STATUS.HELD ||
                seat.status === SEAT_STATUS.BOOKED
        );

        const hasOverride = effectivePrice !== seatMap.ticketPrice;

        const rowEl = document.createElement("div");
        rowEl.className = "row-price-item";

        rowEl.innerHTML = `
            <div class="row-price-item-header">
                <strong>${unit} ${escapeHtml(row.rowLabel)}</strong>
                ${hasOverride ? '<span class="row-price-override-badge">Override active</span>' : ""}
            </div>
            <div class="row-price-item-detail">
                Default: ${formatMoney(seatMap.ticketPrice)} &nbsp;|&nbsp;
                Effective: ${formatMoney(effectivePrice)}
            </div>
            <div class="row-price-item-controls">
                <input
                    type="number"
                    min="0"
                    step="0.01"
                    class="form-control row-price-input"
                    value="${effectivePrice}"
                    ${isProtected ? "disabled" : ""}>
                <button type="button" class="btn btn-primary btn-small row-price-save" ${isProtected ? "disabled" : ""}>
                    Save
                </button>
                <button type="button" class="btn btn-secondary btn-small row-price-reset" ${isProtected ? "disabled" : ""}>
                    Reset to Default
                </button>
            </div>
            ${isProtected ? `<p class="form-footer-text">This ${unit.toLowerCase()} has Held or Booked seats and cannot be repriced.</p>` : ""}
            <div class="feedback row-price-feedback" hidden></div>
        `;

        const input = rowEl.querySelector(".row-price-input");
        const saveButton = rowEl.querySelector(".row-price-save");
        const resetButton = rowEl.querySelector(".row-price-reset");
        const feedbackEl = rowEl.querySelector(".row-price-feedback");

        saveButton.addEventListener("click", () =>
            updateRowPrice(row.rowLabel, Number(input.value), feedbackEl, saveButton));

        resetButton.addEventListener("click", () =>
            updateRowPrice(row.rowLabel, null, feedbackEl, resetButton));

        rowPriceEditor.appendChild(rowEl);
    });
}


async function updateRowPrice(rowLabel, rowPrice, feedbackEl, triggerButton) {
    if (rowPrice !== null && (Number.isNaN(rowPrice) || rowPrice < 0)) {
        showRowFeedback(feedbackEl, "Enter a valid non-negative price.", "error");
        return;
    }

    try {
        setButtonLoading(triggerButton, true, "Saving...");

        await api.put(
            `/api/events/${currentEventId}/seats/row-price?rowLabel=${encodeURIComponent(rowLabel)}`,
            { rowPrice }
        );

        await loadSeatMapForEvent();
    } catch (error) {
        showRowFeedback(
            feedbackEl,
            error?.data?.message || error.message || "Unable to update row price.",
            "error"
        );
        setButtonLoading(triggerButton, false);
    }
}


function showRowFeedback(el, message, type) {
    el.textContent = message;
    el.className = `feedback feedback-${type} row-price-feedback`;
    el.hidden = false;
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
