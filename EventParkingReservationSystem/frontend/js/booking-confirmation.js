import { api } from "./api.js";
import { API_BASE_URL } from "./config.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

import {
    isAuthenticated,
    getCustomerRole,
    getToken
} from "./auth.js";


const authGuardPanel = document.getElementById("authGuardPanel");
const authGuardMessage = document.getElementById("authGuardMessage");
const authGuardLink = document.getElementById("authGuardLink");
const notConfirmedPanel = document.getElementById("notConfirmedPanel");
const notConfirmedMessage = document.getElementById("notConfirmedMessage");
const notConfirmedLink = document.getElementById("notConfirmedLink");
const confirmationLayout = document.getElementById("confirmationLayout");

const bookingNumberEl = document.getElementById("bookingNumber");
const ticketEventName = document.getElementById("ticketEventName");
const ticketEventMeta = document.getElementById("ticketEventMeta");
const ticketSeatsList = document.getElementById("ticketSeatsList");
const ticketParkingSection = document.getElementById("ticketParkingSection");
const ticketParkingRow = document.getElementById("ticketParkingRow");
const ticketTotal = document.getElementById("ticketTotal");
const paymentReference = document.getElementById("paymentReference");
const viewBookingLink = document.getElementById("viewBookingLink");
const downloadReceiptButton = document.getElementById("downloadReceiptButton");
const receiptFeedback = document.getElementById("receiptFeedback");

const foodStallSelect = document.getElementById("foodStallSelect");
const foodItemsList = document.getElementById("foodItemsList");
const pickupTimeGroup = document.getElementById("pickupTimeGroup");
const pickupTimeInput = document.getElementById("pickupTime");
const foodOrderFeedback = document.getElementById("foodOrderFeedback");
const placeFoodOrderButton = document.getElementById("placeFoodOrderButton");
const myFoodOrders = document.getElementById("myFoodOrders");

let bookingId = null;
let booking = null;
let eventItem = null;
let payment = null;
let foodStalls = [];
let foodItems = [];


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
            "Please log in with a customer account to view this confirmation.",
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

    await loadConfirmation();
}


function showAuthGuard(message, linkHref) {
    authGuardMessage.textContent = message;
    authGuardLink.href = linkHref;
    authGuardPanel.hidden = false;
}


async function loadConfirmation() {
    try {
        hideFeedback();

        booking = await api.get(`/api/bookings/${bookingId}`);

        if (booking.status !== "Confirmed") {
            notConfirmedMessage.textContent =
                `This booking is ${booking.status.toLowerCase()}, not confirmed yet. ` +
                `View the booking summary to complete payment or check its status.`;
            notConfirmedLink.href = `./booking-summary.html?id=${bookingId}`;
            notConfirmedPanel.hidden = false;
            return;
        }

        [eventItem, payment] = await Promise.all([
            api.get(`/api/Events/${booking.eventId}`),
            api.get(`/api/bookings/${bookingId}/payment`)
        ]);

        renderConfirmation();
        confirmationLayout.hidden = false;

        await loadFoodStalls();
        await loadMyFoodOrders();
    } catch (error) {
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
            error.message || "Unable to load this booking confirmation.",
            "error"
        );
    }
}


function renderConfirmation() {
    bookingNumberEl.textContent = booking.bookingNumber;

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
    } else {
        ticketParkingSection.hidden = true;
    }

    ticketTotal.textContent = formatMoney(booking.totalAmount);

    paymentReference.textContent = payment?.transactionReference
        ? `Payment reference: ${payment.transactionReference}`
        : "";

    viewBookingLink.href = `./booking-summary.html?id=${bookingId}`;

    downloadReceiptButton.disabled = payment?.status !== "Completed";
}


async function downloadReceipt() {
    if (!payment?.id) {
        return;
    }

    try {
        setButtonLoading(downloadReceiptButton, true, "Preparing...");
        hideReceiptFeedback();

        const token = getToken();

        const response = await fetch(
            `${API_BASE_URL}/api/payments/${payment.id}/receipt`,
            {
                headers: token ? { Authorization: `Bearer ${token}` } : {}
            }
        );

        if (!response.ok) {
            throw new Error(
                response.status === 404
                    ? "Receipt not found."
                    : "Unable to download the receipt right now."
            );
        }

        const blob = await response.blob();
        const url = URL.createObjectURL(blob);

        const link = document.createElement("a");
        link.href = url;
        link.download = `${booking.bookingNumber}-receipt.pdf`;
        document.body.appendChild(link);
        link.click();
        link.remove();

        URL.revokeObjectURL(url);
    } catch (error) {
        showReceiptFeedback(
            error.message || "Unable to download the receipt right now.",
            "error"
        );
    } finally {
        setButtonLoading(downloadReceiptButton, false);
    }
}


/* ---------------- Optional Food (post-confirmation only) ---------------- */

async function loadFoodStalls() {
    try {
        foodStalls = await api.get(`/api/events/${booking.eventId}/food-stalls`);
    } catch {
        foodStalls = [];
    }

    foodStallSelect.innerHTML = `<option value="">Select a food stall</option>`;

    foodStalls.forEach((stall) => {
        const isOpen = stall.status === "Open";
        const option = document.createElement("option");
        option.value = stall.id;
        option.textContent = isOpen ? stall.name : `${stall.name} (${stall.status})`;
        option.disabled = !isOpen;
        foodStallSelect.appendChild(option);
    });

    if (!foodStalls.length) {
        foodStallSelect.disabled = true;
        showFoodOrderFeedback(
            "No food stalls are set up for this event yet.",
            "info"
        );
    }
}


async function handleFoodStallChange() {
    const foodStallId = Number(foodStallSelect.value);

    hideFoodOrderFeedback();
    foodItemsList.innerHTML = "";
    pickupTimeGroup.hidden = true;
    placeFoodOrderButton.hidden = true;

    if (!foodStallId) {
        return;
    }

    try {
        foodItems = await api.get(
            `/api/food-stalls/${foodStallId}/items?availableOnly=true`
        );
    } catch (error) {
        showFoodOrderFeedback(
            error.message || "Unable to load the menu for this stall.",
            "error"
        );
        return;
    }

    if (!foodItems.length) {
        foodItemsList.innerHTML =
            `<p class="selected-seats-empty">No items are available at this stall right now.</p>`;
        return;
    }

    foodItemsList.innerHTML = foodItems.map((item) => `
        <div class="food-item-row" data-food-item-id="${item.id}">
            <div>
                <strong>${escapeHtml(item.name)}</strong>
                <span class="form-footer-text">${formatMoney(item.price)}</span>
            </div>
            <input
                type="number"
                min="0"
                step="1"
                value="0"
                class="form-control food-item-quantity"
                aria-label="Quantity for ${escapeHtml(item.name)}">
        </div>
    `).join("");

    pickupTimeGroup.hidden = false;

    if (eventItem) {
        pickupTimeInput.min = toDateTimeLocal(eventItem.startDateTime);
        pickupTimeInput.max = toDateTimeLocal(eventItem.endDateTime);
    }

    placeFoodOrderButton.hidden = false;
}


async function handlePlaceFoodOrder() {
    const foodStallId = Number(foodStallSelect.value);

    if (!foodStallId) {
        showFoodOrderFeedback("Select a food stall first.", "error");
        return;
    }

    const items = Array.from(
        foodItemsList.querySelectorAll(".food-item-row")
    ).map((row) => ({
        foodItemId: Number(row.dataset.foodItemId),
        quantity: Number(row.querySelector(".food-item-quantity").value) || 0
    })).filter((item) => item.quantity > 0);

    if (!items.length) {
        showFoodOrderFeedback(
            "Enter a quantity for at least one item.",
            "error"
        );
        return;
    }

    if (!pickupTimeInput.value) {
        showFoodOrderFeedback("Choose a pickup time.", "error");
        return;
    }

    try {
        setButtonLoading(placeFoodOrderButton, true, "Placing order...");
        hideFoodOrderFeedback();

        await api.post("/api/food-orders", {
            bookingId,
            foodStallId,
            pickupTime: pickupTimeInput.value,
            items
        });

        showFoodOrderFeedback("Food order placed successfully.", "success");

        foodStallSelect.value = "";
        foodItemsList.innerHTML = "";
        pickupTimeGroup.hidden = true;
        placeFoodOrderButton.hidden = true;

        await loadMyFoodOrders();
    } catch (error) {
        showFoodOrderFeedback(
            error?.data?.message ||
            error.message ||
            "Unable to place this food order.",
            "error"
        );
    } finally {
        setButtonLoading(placeFoodOrderButton, false);
    }
}


async function loadMyFoodOrders() {
    try {
        const orders = await api.get("/api/food-orders/my-orders");

        const bookingOrders = orders.filter(
            (order) => order.bookingId === bookingId
        );

        if (!bookingOrders.length) {
            myFoodOrders.innerHTML = "";
            return;
        }

        myFoodOrders.innerHTML = `
            <h4>Your Food Orders</h4>
            ${bookingOrders.map((order) => `
                <div class="food-order-summary">
                    <div class="selected-seat-row">
                        <strong>${escapeHtml(order.orderNumber)}</strong>
                        <span class="status-badge">${escapeHtml(order.status)}</span>
                    </div>
                    ${order.items.map((item) => `
                        <div class="selected-seat-row">
                            <span>${escapeHtml(item.foodItemName)} &times; ${item.quantity}</span>
                            <span>${formatMoney(item.lineTotal)}</span>
                        </div>
                    `).join("")}
                    <div class="selected-seat-row">
                        <span>Pickup</span>
                        <span>${formatDate(order.pickupTime)} ${formatTime(order.pickupTime)}</span>
                    </div>
                    <div class="selected-seats-total">
                        <span>Total</span>
                        <span>${formatMoney(order.totalAmount)}</span>
                    </div>
                </div>
            `).join("")}
        `;
    } catch {
        // Non-critical: leave the previous list state on transient failure.
    }
}


function showFoodOrderFeedback(message, type) {
    foodOrderFeedback.textContent = message;
    foodOrderFeedback.className = `feedback feedback-${type}`;
    foodOrderFeedback.hidden = false;
}


function hideFoodOrderFeedback() {
    foodOrderFeedback.hidden = true;
    foodOrderFeedback.textContent = "";
}


function showReceiptFeedback(message, type) {
    receiptFeedback.textContent = message;
    receiptFeedback.className = `feedback feedback-${type}`;
    receiptFeedback.hidden = false;
}


function hideReceiptFeedback() {
    receiptFeedback.hidden = true;
    receiptFeedback.textContent = "";
}


function toDateTimeLocal(value) {
    if (!value) {
        return "";
    }

    return String(value).slice(0, 16);
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


downloadReceiptButton.addEventListener("click", downloadReceipt);
foodStallSelect.addEventListener("change", handleFoodStallChange);
placeFoodOrderButton.addEventListener("click", handlePlaceFoodOrder);
