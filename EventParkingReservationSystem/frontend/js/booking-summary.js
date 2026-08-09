import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

import { isAuthenticated, getCustomerRole } from "./auth.js";


const authGuardPanel = document.getElementById("authGuardPanel");
const authGuardMessage = document.getElementById("authGuardMessage");
const authGuardLink = document.getElementById("authGuardLink");
const bookingLayout = document.getElementById("bookingLayout");

const bookingNumberEl = document.getElementById("bookingNumber");
const bookingStatusBadge = document.getElementById("bookingStatusBadge");
const holdCountdownBlock = document.getElementById("holdCountdownBlock");
const holdCountdown = document.getElementById("holdCountdown");

const ticketEventName = document.getElementById("ticketEventName");
const ticketEventMeta = document.getElementById("ticketEventMeta");
const ticketSeatsList = document.getElementById("ticketSeatsList");
const ticketParkingSection = document.getElementById("ticketParkingSection");
const ticketParkingRow = document.getElementById("ticketParkingRow");
const ticketSeatTotal = document.getElementById("ticketSeatTotal");
const ticketParkingTotalRow = document.getElementById("ticketParkingTotalRow");
const ticketParkingTotal = document.getElementById("ticketParkingTotal");
const ticketTotal = document.getElementById("ticketTotal");

const paymentFeedback = document.getElementById("paymentFeedback");
const pendingActions = document.getElementById("pendingActions");
const payNowButton = document.getElementById("payNowButton");
const cancelBookingButton = document.getElementById("cancelBookingButton");
const confirmedActions = document.getElementById("confirmedActions");
const viewConfirmationLink = document.getElementById("viewConfirmationLink");
const closedActions = document.getElementById("closedActions");
const chooseSeatsAgainLink = document.getElementById("chooseSeatsAgainLink");

let bookingId = null;
let remainingSeconds = 0;
let tickTimer = null;
let pollTimer = null;

const POLL_INTERVAL_MS = 20000;


document.addEventListener("DOMContentLoaded", async () => {
    renderNavbar();
    await initializePage();
});


async function initializePage() {
    const parameters = new URLSearchParams(window.location.search);
    bookingId = Number(parameters.get("id"));

    if (!bookingId || bookingId <= 0) {
        showFeedback("Invalid booking selected.", "error");
        return;
    }

    if (!isAuthenticated()) {
        showAuthGuard(
            "Please log in with a customer account to view this booking.",
            "login.html"
        );
        return;
    }

    if (getCustomerRole() !== "Customer") {
        showAuthGuard(
            "This checkout flow is only available to customer accounts.",
            "manage-events.html"
        );
        return;
    }

    await loadBooking();
}


function showAuthGuard(message, linkHref) {
    authGuardMessage.textContent = message;
    authGuardLink.href = linkHref;
    authGuardPanel.hidden = false;
}


async function loadBooking() {
    try {
        hideFeedback();
        stopTimers();

        const booking = await api.get(`/api/bookings/${bookingId}`);

        renderBooking(booking);
        bookingLayout.hidden = false;
    } catch (error) {
        bookingLayout.hidden = true;

        if (error?.status === 404) {
            showFeedback("Booking not found.", "error");
            return;
        }

        if (error?.status === 401) {
            showAuthGuard(
                "Your session has expired. Please log in again.",
                "login.html"
            );
            return;
        }

        if (error?.status === 403) {
            showFeedback(
                "This booking does not belong to your account.",
                "error"
            );
            return;
        }

        showFeedback(
            error.message || "Unable to load this booking.",
            "error"
        );
    }
}


function renderBooking(booking) {
    bookingNumberEl.textContent = booking.bookingNumber;
    bookingStatusBadge.textContent = booking.status;
    bookingStatusBadge.className =
        `status-badge ${statusBadgeClass(booking.status)}`;

    ticketEventName.textContent = booking.eventName;
    ticketEventMeta.textContent =
        `${formatDate(booking.eventStartDateTime)} | ` +
        `${formatTime(booking.eventStartDateTime)}`;

    ticketSeatsList.innerHTML = booking.seats.map((seat) => `
        <div class="selected-seat-row">
            <span>${escapeHtml(seat.seatNumber)}</span>
            <span>${formatMoney(seat.unitPriceAtBooking)}</span>
        </div>
    `).join("");

    if (booking.parking) {
        ticketParkingSection.hidden = false;
        ticketParkingRow.innerHTML = `
            <span>
                Slot ${escapeHtml(booking.parking.slotNumber)}
                ${booking.parking.zone ? `&middot; ${escapeHtml(booking.parking.zone)}` : ""}
            </span>
            <span>${formatMoney(booking.parking.feeAtReservation)}</span>
        `;
        ticketParkingTotalRow.hidden = false;
        ticketParkingTotal.textContent = formatMoney(booking.parkingTotal);
    } else {
        ticketParkingSection.hidden = true;
        ticketParkingTotalRow.hidden = true;
    }

    ticketSeatTotal.textContent = formatMoney(booking.seatTotal);
    ticketTotal.textContent = formatMoney(booking.totalAmount);

    pendingActions.hidden = true;
    confirmedActions.hidden = true;
    closedActions.hidden = true;
    holdCountdownBlock.hidden = true;

    if (booking.status === "Pending") {
        remainingSeconds = booking.remainingHoldSeconds || 0;
        updateCountdownDisplay();
        holdCountdownBlock.hidden = false;
        pendingActions.hidden = false;
        startTimers();
    } else if (booking.status === "Confirmed") {
        confirmedActions.hidden = false;
        viewConfirmationLink.href = `./booking-confirmation.html?id=${booking.id}`;
    } else {
        closedActions.hidden = false;
        chooseSeatsAgainLink.href = `./event-details.html?id=${booking.eventId}`;
    }
}


function statusBadgeClass(status) {
    switch (status) {
        case "Confirmed":
            return "status-confirmed";
        case "Pending":
            return "status-pending";
        case "Cancelled":
        case "Expired":
            return "status-cancelled";
        default:
            return "";
    }
}


function startTimers() {
    stopTimers();

    tickTimer = window.setInterval(() => {
        remainingSeconds = Math.max(0, remainingSeconds - 1);
        updateCountdownDisplay();

        if (remainingSeconds === 0) {
            refreshHoldStatus();
        }
    }, 1000);

    pollTimer = window.setInterval(refreshHoldStatus, POLL_INTERVAL_MS);
}


function stopTimers() {
    if (tickTimer) {
        window.clearInterval(tickTimer);
        tickTimer = null;
    }

    if (pollTimer) {
        window.clearInterval(pollTimer);
        pollTimer = null;
    }
}


function updateCountdownDisplay() {
    const minutes = Math.floor(remainingSeconds / 60);
    const seconds = remainingSeconds % 60;

    holdCountdown.textContent =
        `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}


async function refreshHoldStatus() {
    try {
        const status = await api.get(`/api/bookings/${bookingId}/hold-status`);

        if (status.status !== "Pending") {
            stopTimers();
            await loadBooking();
            return;
        }

        remainingSeconds = status.remainingSeconds;
        updateCountdownDisplay();
        payNowButton.disabled = !status.canPay;
    } catch {
        // Keep showing the last known state; the next poll will retry.
    }
}


async function handlePayNow() {
    try {
        setButtonLoading(payNowButton, true, "Processing payment...");
        cancelBookingButton.disabled = true;
        hidePaymentFeedback();

        const payment = await api.post(`/api/bookings/${bookingId}/payment`, {
            simulateSuccess: true
        });

        if (payment.status !== "Completed") {
            showPaymentFeedback(
                "Payment was not completed. Please try again.",
                "error"
            );
            await loadBooking();
            return;
        }

        window.location.assign(`./booking-confirmation.html?id=${bookingId}`);
    } catch (error) {
        showPaymentFeedback(
            error?.data?.message ||
            error.message ||
            "Payment could not be processed. Please try again.",
            "error"
        );

        await loadBooking();
    } finally {
        setButtonLoading(payNowButton, false);
        cancelBookingButton.disabled = false;
    }
}


async function handleCancelBooking() {
    const confirmed = window.confirm(
        "Cancel this booking and release your held seats?"
    );

    if (!confirmed) {
        return;
    }

    try {
        setButtonLoading(cancelBookingButton, true, "Cancelling...");
        payNowButton.disabled = true;
        hidePaymentFeedback();

        await api.delete(`/api/bookings/${bookingId}`);

        await loadBooking();
    } catch (error) {
        showPaymentFeedback(
            error?.data?.message ||
            error.message ||
            "Unable to cancel this booking.",
            "error"
        );
    } finally {
        setButtonLoading(cancelBookingButton, false);
        payNowButton.disabled = false;
    }
}


function showPaymentFeedback(message, type) {
    paymentFeedback.textContent = message;
    paymentFeedback.className = `feedback feedback-${type}`;
    paymentFeedback.hidden = false;
}


function hidePaymentFeedback() {
    paymentFeedback.hidden = true;
    paymentFeedback.textContent = "";
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


payNowButton.addEventListener("click", handlePayNow);
cancelBookingButton.addEventListener("click", handleCancelBooking);
