import { api } from "./api.js";

import {
    showFeedback,
    hideFeedback
} from "./ui.js";

import {
    requireAdministrator,
    renderAdminSidebar,
    setupAdminMobileMenu
} from "./admin-ui.js";


/*
 * BookingStatus mirrors EventParking.Models.Enums.BookingStatus exactly
 * (Pending = 1, Confirmed = 2, Cancelled = 3, Expired = 4). BookingResponse
 * already returns Status as a friendly string, and - unlike Parking/Food/
 * Facilities - also already embeds CustomerName and EventName directly
 * (BookingService.MapBooking resolves them server-side), so this page
 * never needs a separate Events/Customers lookup call.
 *
 * The only Administrator mutation available is DELETE /api/bookings/{id}
 * (BookingsController.Cancel -> BookingService.CancelPendingAsync), and
 * only while the booking is still Pending AND its hold has not expired
 * (RemainingHoldSeconds > 0) - there is no Confirm/UpdateStatus endpoint
 * for Administrators. The Cancel action below is only ever shown for
 * bookings meeting exactly that real condition.
 */


const adminShell = document.getElementById("adminShell");
const bookingAdminBody = document.getElementById("bookingAdminBody");
const bookingsLoadingState = document.getElementById("bookingsLoadingState");

const statTotalBookings = document.getElementById("statTotalBookings");
const statPendingBookings = document.getElementById("statPendingBookings");
const statConfirmedBookings = document.getElementById("statConfirmedBookings");
const statExpiredBookings = document.getElementById("statExpiredBookings");
const statCancelledBookings = document.getElementById("statCancelledBookings");

const bookingSearchInput = document.getElementById("bookingSearchInput");
const bookingStatusFilter = document.getElementById("bookingStatusFilter");
const bookingEventFilter = document.getElementById("bookingEventFilter");
const bookingSortSelect = document.getElementById("bookingSortSelect");
const clearBookingFiltersButton = document.getElementById("clearBookingFiltersButton");

const bookingActionFeedback = document.getElementById("bookingActionFeedback");
const bookingList = document.getElementById("bookingList");


let bookings = [];

let searchTerm = "";
let statusFilterValue = "";
let eventFilterValue = "";
let sortValue = "newest";


document.addEventListener("DOMContentLoaded", async () => {
    if (!requireAdministrator()) {
        return;
    }

    renderAdminSidebar("bookings");
    setupAdminMobileMenu();

    adminShell.hidden = false;

    await initializePage();
});


async function initializePage() {
    try {
        hideFeedback();
        bookingsLoadingState.hidden = false;
        bookingAdminBody.hidden = true;

        bookings = await api.get("/api/bookings");

        bookingsLoadingState.hidden = true;
        bookingAdminBody.hidden = false;

        populateEventFilterOptions();
        applyEventIdFromUrl();

        renderSummary();
        renderBookingList();
    } catch (error) {
        bookingsLoadingState.hidden = true;

        showFeedback(
            error?.data?.message || error.message || "Unable to load bookings.",
            "error"
        );
    }
}


/*
 * Built entirely from the already-loaded bookings (each carries its own
 * real EventId + EventName) - no separate /api/Events call is made.
 */
function populateEventFilterOptions() {
    const uniqueEvents = new Map();

    bookings.forEach((booking) => {
        if (!uniqueEvents.has(booking.eventId)) {
            uniqueEvents.set(booking.eventId, booking.eventName);
        }
    });

    const sortedEvents = Array.from(uniqueEvents.entries())
        .sort((a, b) => a[1].localeCompare(b[1]));

    bookingEventFilter.innerHTML = `<option value="">All events</option>`;

    sortedEvents.forEach(([eventId, eventName]) => {
        const option = document.createElement("option");
        option.value = String(eventId);
        option.textContent = eventName;
        bookingEventFilter.appendChild(option);
    });
}


/*
 * Supports manage-bookings.html?eventId={eventId} (from the Admin
 * Dashboard's per-event "View Bookings" action). All bookings are still
 * loaded once via the real GET /api/bookings - this only preselects the
 * client-side Event filter, it never calls a per-event booking endpoint.
 */
function applyEventIdFromUrl() {
    const requestedEventId =
        Number(new URLSearchParams(window.location.search).get("eventId"));

    if (!requestedEventId) {
        return;
    }

    const hasOption = Array.from(bookingEventFilter.options).some(
        (option) => option.value === String(requestedEventId)
    );

    if (!hasOption) {
        const matchingBooking = bookings.find(
            (booking) => booking.eventId === requestedEventId
        );

        const option = document.createElement("option");
        option.value = String(requestedEventId);
        option.textContent = matchingBooking
            ? matchingBooking.eventName
            : `Event #${requestedEventId}`;

        bookingEventFilter.appendChild(option);
    }

    bookingEventFilter.value = String(requestedEventId);
    eventFilterValue = String(requestedEventId);
}


/* ---------------- Real, client-derived summary (unfiltered totals) ---------------- */

function renderSummary() {
    statTotalBookings.textContent = bookings.length;

    statPendingBookings.textContent = bookings.filter(
        (booking) => booking.status === "Pending"
    ).length;

    statConfirmedBookings.textContent = bookings.filter(
        (booking) => booking.status === "Confirmed"
    ).length;

    statExpiredBookings.textContent = bookings.filter(
        (booking) => booking.status === "Expired"
    ).length;

    statCancelledBookings.textContent = bookings.filter(
        (booking) => booking.status === "Cancelled"
    ).length;
}


/* ---------------- Search + Filter + Sort (client-side only) ---------------- */

function getFilteredSortedBookings() {
    const term = searchTerm.trim().toLowerCase();

    const filtered = bookings.filter((booking) => {
        const matchesStatus =
            !statusFilterValue || booking.status === statusFilterValue;

        const matchesEvent =
            !eventFilterValue || String(booking.eventId) === eventFilterValue;

        const matchesSearch =
            !term ||
            String(booking.id).includes(term) ||
            booking.bookingNumber.toLowerCase().includes(term) ||
            booking.customerName.toLowerCase().includes(term) ||
            String(booking.customerId).includes(term) ||
            booking.eventName.toLowerCase().includes(term) ||
            String(booking.eventId).includes(term);

        return matchesStatus && matchesEvent && matchesSearch;
    });

    const sorted = filtered.slice();

    if (sortValue === "oldest") {
        sorted.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt));
    } else if (sortValue === "eventDate") {
        sorted.sort((a, b) => new Date(a.eventStartDateTime) - new Date(b.eventStartDateTime));
    } else {
        sorted.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
    }

    return sorted;
}


bookingSearchInput.addEventListener("input", () => {
    searchTerm = bookingSearchInput.value;
    renderBookingList();
});


bookingStatusFilter.addEventListener("change", () => {
    statusFilterValue = bookingStatusFilter.value;
    renderBookingList();
});


bookingEventFilter.addEventListener("change", () => {
    eventFilterValue = bookingEventFilter.value;
    renderBookingList();
});


bookingSortSelect.addEventListener("change", () => {
    sortValue = bookingSortSelect.value;
    renderBookingList();
});


clearBookingFiltersButton.addEventListener("click", () => {
    searchTerm = "";
    statusFilterValue = "";
    eventFilterValue = "";
    sortValue = "newest";

    bookingSearchInput.value = "";
    bookingStatusFilter.value = "";
    bookingEventFilter.value = "";
    bookingSortSelect.value = "newest";

    renderBookingList();
});


/* ---------------- Booking list ---------------- */

function renderBookingList() {
    bookingList.innerHTML = "";

    if (!bookings.length) {
        bookingList.innerHTML = `
            <p class="admin-empty-note">
                No bookings found.
            </p>
        `;
        return;
    }

    const filtered = getFilteredSortedBookings();

    if (!filtered.length) {
        bookingList.innerHTML = `
            <p class="admin-empty-note">
                No bookings match your current filters.
            </p>
        `;
        return;
    }

    filtered.forEach((booking) => {
        bookingList.appendChild(buildBookingRow(booking));
    });
}


function buildBookingRow(booking) {
    const row = document.createElement("div");
    row.className = "booking-row";

    const canCancel =
        booking.status === "Pending" && booking.remainingHoldSeconds > 0;

    row.innerHTML = `
        <div class="booking-header">
            <span class="booking-number">${escapeHtml(booking.bookingNumber)}</span>
            <span class="booking-status-badge booking-status-${booking.status.toLowerCase()}">
                ${escapeHtml(booking.status)}
            </span>
            <span class="booking-time">Created ${formatDateTime(booking.createdAt)}</span>
        </div>

        <div class="booking-meta">
            <span><strong>Customer:</strong> ${escapeHtml(booking.customerName)} (#${booking.customerId})</span>
            <span><strong>Event:</strong> ${escapeHtml(booking.eventName)} (#${booking.eventId})</span>
            <span><strong>Event date:</strong> ${formatDateTime(booking.eventStartDateTime)}</span>
        </div>

        <div class="booking-footer">
            <span class="booking-total">Total: ${formatMoney(booking.totalAmount)}</span>
            ${renderPaymentBadge(booking.paymentStatus)}

            <div class="booking-actions">
                <button type="button" class="btn btn-secondary btn-small view-details-button">
                    View Details
                </button>
                ${canCancel
                    ? `<button type="button" class="btn btn-secondary btn-small cancel-booking-button" data-id="${booking.id}">Cancel Booking</button>`
                    : ""
                }
            </div>
        </div>

        <div class="booking-detail-panel" hidden>
            ${buildBookingDetailHtml(booking)}
        </div>
    `;

    const detailPanel = row.querySelector(".booking-detail-panel");
    const viewDetailsButton = row.querySelector(".view-details-button");

    viewDetailsButton.addEventListener("click", () => {
        const isHidden = detailPanel.hidden;
        detailPanel.hidden = !isHidden;
        viewDetailsButton.textContent = isHidden ? "Hide Details" : "View Details";
    });

    const cancelButton = row.querySelector(".cancel-booking-button");

    if (cancelButton) {
        cancelButton.addEventListener("click", () => cancelBooking(booking));
    }

    return row;
}


function renderPaymentBadge(paymentStatus) {
    if (!paymentStatus) {
        return `<span class="booking-payment-badge is-none">No payment yet</span>`;
    }

    return `
        <span class="booking-payment-badge is-${paymentStatus.toLowerCase()}">
            Payment: ${escapeHtml(paymentStatus)}
        </span>
    `;
}


function buildBookingDetailHtml(booking) {
    const seatsHtml = booking.seats.length
        ? `
            <div class="booking-seats-list">
                ${booking.seats.map((seat) => `<span class="booking-seat-chip">${escapeHtml(seat.seatNumber)}</span>`).join("")}
            </div>
          `
        : `<p class="form-footer-text">No seats found.</p>`;

    const parkingHtml = booking.parking
        ? `
            <div class="booking-detail-grid">
                <div>
                    <span class="booking-detail-label">Parking Slot</span>
                    <span class="booking-detail-value">${escapeHtml(booking.parking.slotNumber)}</span>
                </div>
                ${booking.parking.zone ? `
                    <div>
                        <span class="booking-detail-label">Zone</span>
                        <span class="booking-detail-value">${escapeHtml(booking.parking.zone)}</span>
                    </div>
                ` : ""}
                <div>
                    <span class="booking-detail-label">Parking Fee</span>
                    <span class="booking-detail-value">${formatMoney(booking.parking.feeAtReservation)}</span>
                </div>
            </div>
          `
        : `<p class="form-footer-text">No parking reserved.</p>`;

    return `
        <div class="booking-detail-grid">

            <div>
                <span class="booking-detail-label">Booking ID</span>
                <span class="booking-detail-value">#${booking.id}</span>
            </div>

            <div>
                <span class="booking-detail-label">Seat Total</span>
                <span class="booking-detail-value">${formatMoney(booking.seatTotal)}</span>
            </div>

            <div>
                <span class="booking-detail-label">Parking Total</span>
                <span class="booking-detail-value">${formatMoney(booking.parkingTotal)}</span>
            </div>

            ${booking.status === "Pending" && booking.holdExpiresAt ? `
                <div>
                    <span class="booking-detail-label">Hold Expires At</span>
                    <span class="booking-detail-value">${formatDateTime(booking.holdExpiresAt)}</span>
                </div>
            ` : ""}

            ${booking.cancelledAt ? `
                <div>
                    <span class="booking-detail-label">Cancelled At</span>
                    <span class="booking-detail-value">${formatDateTime(booking.cancelledAt)}</span>
                </div>
            ` : ""}

        </div>

        <p class="booking-detail-label" style="margin-bottom: 6px;">Seats</p>
        ${seatsHtml}

        <p class="booking-detail-label" style="margin: 14px 0 6px;">Parking</p>
        ${parkingHtml}
    `;
}


async function cancelBooking(booking) {
    const confirmed = window.confirm(
        `Cancel booking ${booking.bookingNumber}? This releases its held seats and parking.`
    );

    if (!confirmed) {
        return;
    }

    try {
        hideBookingActionFeedback();

        await api.delete(`/api/bookings/${booking.id}`);

        showBookingActionFeedback(
            `Booking ${booking.bookingNumber} was cancelled.`,
            "success"
        );

        bookings = await api.get("/api/bookings");

        populateEventFilterOptions();
        renderSummary();
        renderBookingList();
    } catch (error) {
        showBookingActionFeedback(
            error?.data?.message || error.message || "Unable to cancel this booking.",
            "error"
        );
    }
}


function showBookingActionFeedback(message, type) {
    bookingActionFeedback.textContent = message;
    bookingActionFeedback.className = `feedback feedback-${type}`;
    bookingActionFeedback.hidden = false;
}


function hideBookingActionFeedback() {
    bookingActionFeedback.hidden = true;
    bookingActionFeedback.textContent = "";
}


function formatDateTime(value) {
    if (!value) {
        return "-";
    }

    return new Date(value).toLocaleString();
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
