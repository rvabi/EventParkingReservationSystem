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
    groupSeatsByRow
} from "./seat-map.js";


const eventSelect = document.getElementById("eventSelect");
const seatManagementPanel = document.getElementById("seatManagementPanel");

const statCapacity = document.getElementById("statCapacity");
const statGenerated = document.getElementById("statGenerated");
const statTicketPrice = document.getElementById("statTicketPrice");

const generatePanel = document.getElementById("generatePanel");
const rowDefinitions = document.getElementById("rowDefinitions");
const addRowButton = document.getElementById("addRowButton");
const rowTotalSummary = document.getElementById("rowTotalSummary");
const generateFeedback = document.getElementById("generateFeedback");
const generateButton = document.getElementById("generateButton");

const existingMapPanel = document.getElementById("existingMapPanel");
const squareLayoutButton = document.getElementById("squareLayoutButton");
const groundLayoutButton = document.getElementById("groundLayoutButton");
const seatMapContainer = document.getElementById("seatMapContainer");
const seatActionFeedback = document.getElementById("seatActionFeedback");
const rowPriceEditor = document.getElementById("rowPriceEditor");


let events = [];
let currentEventId = null;
let seatMap = null;

/*
 * Layout preview is intentionally not persisted anywhere - there is no
 * LayoutType/SeatingLayout field on Event/Venue/Seat in the current
 * backend. This resets to "square" every page load / event switch. See
 * the phase report for what a persisted field would require.
 */
let previewLayout = "square";


document.addEventListener("DOMContentLoaded", async () => {
    renderNavbar();
    await initializePage();
});


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
        seatMap = null;
        return;
    }

    currentEventId = Number(value);
    previewLayout = "square";
    await loadSeatMapForEvent();
});


async function loadSeatMapForEvent() {
    try {
        hideFeedback();
        seatManagementPanel.hidden = true;

        seatMap = await api.get(`/api/events/${currentEventId}/seats`);

        seatManagementPanel.hidden = false;

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
            setActiveLayoutButton();
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
    addRowDefinitionRow();
    addRowDefinitionRow();
    updateRowTotalSummary();
    hideGenerateFeedback();
}


function addRowDefinitionRow() {
    const row = document.createElement("div");
    row.className = "row-definition-item";

    row.innerHTML = `
        <div class="form-group">
            <label class="form-label">Seat Count</label>
            <input type="number" min="1" class="form-control row-seat-count" value="1">
        </div>
        <div class="form-group">
            <label class="form-label">Row Price (optional)</label>
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

    rowTotalSummary.textContent =
        diff === 0
            ? `${total} / ${capacity} seats - matches event capacity.`
            : diff > 0
                ? `${total} / ${capacity} seats - ${diff} seat(s) remaining.`
                : `${total} / ${capacity} seats - ${Math.abs(diff)} seat(s) over capacity.`;

    rowTotalSummary.classList.toggle("row-total-summary-error", diff !== 0);
}


addRowButton.addEventListener("click", addRowDefinitionRow);


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
        showGenerateFeedback("Every row needs at least 1 seat.", "error");
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


/* ---------------- Layout Preview + Seat Status ---------------- */

function renderAdminSeatMap() {
    renderSeatMap(seatMapContainer, {
        seats: seatMap.seats,
        layout: previewLayout,
        mode: "admin",
        onSeatClick: handleAdminSeatClick
    });
}


squareLayoutButton.addEventListener("click", () => {
    previewLayout = "square";
    setActiveLayoutButton();
    renderAdminSeatMap();
});


groundLayoutButton.addEventListener("click", () => {
    previewLayout = "ground";
    setActiveLayoutButton();
    renderAdminSeatMap();
});


function setActiveLayoutButton() {
    squareLayoutButton.classList.toggle("btn-primary", previewLayout === "square");
    squareLayoutButton.classList.toggle("btn-secondary", previewLayout !== "square");
    groundLayoutButton.classList.toggle("btn-primary", previewLayout === "ground");
    groundLayoutButton.classList.toggle("btn-secondary", previewLayout !== "ground");
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


/* ---------------- Row Price Editor ---------------- */

function renderRowPriceEditor() {
    const rows = groupSeatsByRow(seatMap.seats);

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
                <strong>Row ${escapeHtml(row.rowLabel)}</strong>
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
            ${isProtected ? '<p class="form-footer-text">This row has Held or Booked seats and cannot be repriced.</p>' : ""}
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
