import { api } from "./api.js";

import {
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

import {
    requireAdministrator,
    renderAdminSidebar,
    setupAdminMobileMenu,
    getSetupParams,
    renderSetupContextBar
} from "./admin-ui.js";


/*
 * FoodOrderStatus mirrors EventParking.Models.Enums.FoodOrderStatus exactly
 * (Pending = 1, Confirmed = 2, Preparing = 3, ReadyForPickup = 4,
 * Collected = 5, Cancelled = 6). UpdateFoodOrderStatusRequest.Status has no
 * JsonStringEnumConverter, so it must be sent as this numeric value - the
 * response Status field is already a string (FoodOrderService.MapToResponse
 * calls .ToString()).
 *
 * STATUS_TRANSITIONS mirrors FoodOrderService.IsValidStatusTransition
 * exactly, so the admin queue only ever offers a status-change button for a
 * transition the backend will actually accept (Cancelled is reachable from
 * any non-terminal state; Collected and Cancelled are both terminal).
 */
const FOOD_ORDER_STATUS = {
    Pending: 1,
    Confirmed: 2,
    Preparing: 3,
    ReadyForPickup: 4,
    Collected: 5,
    Cancelled: 6
};

const STATUS_TRANSITIONS = {
    Pending: ["Confirmed", "Cancelled"],
    Confirmed: ["Preparing", "Cancelled"],
    Preparing: ["ReadyForPickup", "Cancelled"],
    ReadyForPickup: ["Collected", "Cancelled"],
    Collected: [],
    Cancelled: []
};

const STATUS_ACTION_LABEL = {
    Confirmed: "Confirm",
    Preparing: "Start Preparing",
    ReadyForPickup: "Mark Ready for Pickup",
    Collected: "Mark Collected",
    Cancelled: "Cancel Order"
};

const STATUS_DISPLAY_LABEL = {
    Pending: "Pending",
    Confirmed: "Confirmed",
    Preparing: "Preparing",
    ReadyForPickup: "Ready For Pickup",
    Collected: "Collected",
    Cancelled: "Cancelled"
};


const adminShell = document.getElementById("adminShell");
const foodAdminBody = document.getElementById("foodAdminBody");
const setupContextBar = document.getElementById("setupContextBar");
const eventPickerGroup = document.getElementById("eventPickerGroup");
const setupNav = document.getElementById("setupNav");
const setupBackButton = document.getElementById("setupBackButton");
const setupContinueButton = document.getElementById("setupContinueButton");

const statTotalStalls = document.getElementById("statTotalStalls");
const statActiveStalls = document.getElementById("statActiveStalls");
const statMenuItems = document.getElementById("statMenuItems");
const statAvailableItems = document.getElementById("statAvailableItems");
const statPendingOrders = document.getElementById("statPendingOrders");

const tabButtons = Array.from(document.querySelectorAll(".admin-tab-button"));
const tabPanels = {
    stalls: document.getElementById("tabPanelStalls"),
    menu: document.getElementById("tabPanelMenu"),
    orders: document.getElementById("tabPanelOrders")
};

/* ---- Stalls tab ---- */
const eventSelect = document.getElementById("eventSelect");
const noEventSelectedHint = document.getElementById("noEventSelectedHint");
const stallsLoadingState = document.getElementById("stallsLoadingState");
const stallsPanel = document.getElementById("stallsPanel");

const foodStallForm = document.getElementById("foodStallForm");
const foodStallIdInput = document.getElementById("foodStallId");
const stallNameInput = document.getElementById("stallName");
const stallDescriptionInput = document.getElementById("stallDescription");
const stallStatusInput = document.getElementById("stallStatus");
const stallFormTitle = document.getElementById("stallFormTitle");
const stallFormFeedback = document.getElementById("stallFormFeedback");
const saveStallButton = document.getElementById("saveStallButton");
const cancelStallEditButton = document.getElementById("cancelStallEditButton");

const foodStallList = document.getElementById("foodStallList");

/* ---- Menu tab ---- */
const noEventSelectedHintMenu = document.getElementById("noEventSelectedHintMenu");
const menuLoadingState = document.getElementById("menuLoadingState");
const menuPanel = document.getElementById("menuPanel");

const foodItemForm = document.getElementById("foodItemForm");
const foodItemIdInput = document.getElementById("foodItemId");
const itemStallIdInput = document.getElementById("itemStallId");
const itemNameInput = document.getElementById("itemName");
const itemDescriptionInput = document.getElementById("itemDescription");
const itemPriceInput = document.getElementById("itemPrice");
const itemAvailableInput = document.getElementById("itemAvailable");
const itemFormTitle = document.getElementById("itemFormTitle");
const itemFormFeedback = document.getElementById("itemFormFeedback");
const saveItemButton = document.getElementById("saveItemButton");
const cancelItemEditButton = document.getElementById("cancelItemEditButton");

const foodMenuList = document.getElementById("foodMenuList");

/* ---- Orders tab ---- */
const ordersLoadingState = document.getElementById("ordersLoadingState");
const ordersPanel = document.getElementById("ordersPanel");
const orderSearchInput = document.getElementById("orderSearchInput");
const orderStatusFilter = document.getElementById("orderStatusFilter");
const clearOrderFiltersButton = document.getElementById("clearOrderFiltersButton");
const orderStatusFeedback = document.getElementById("orderStatusFeedback");
const foodOrderList = document.getElementById("foodOrderList");


let events = [];
let currentEventId = null;
let currentEvent = null;

let stalls = [];
let itemsByStallId = new Map();

let orders = [];
let orderSearchTerm = "";
let orderStatusFilterValue = "";

const setupParams = getSetupParams();


document.addEventListener("DOMContentLoaded", async () => {
    if (!requireAdministrator()) {
        return;
    }

    renderAdminSidebar("food");
    setupAdminMobileMenu();

    adminShell.hidden = false;
    foodAdminBody.hidden = false;

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
            option.textContent = eventItem.name;
            eventSelect.appendChild(option);
        });

        const requestedId =
            setupParams.eventId ||
            Number(new URLSearchParams(window.location.search).get("id"));

        if (requestedId && events.some((eventItem) => eventItem.id === requestedId)) {
            eventSelect.value = String(requestedId);
            currentEventId = requestedId;
            currentEvent = events.find((eventItem) => eventItem.id === requestedId);

            if (setupParams.isSetup) {
                await activateSetupMode();
            }

            await loadStalls();
            await loadItemsForStalls();
        }
    } catch (error) {
        showFeedback(
            error.message || "Unable to load events.",
            "error"
        );
    }

    await loadOrdersQueue();
    renderSummary();
}


/*
 * Continuous Event Setup Flow (Corrections 8-13): only active via
 * manage-food.html?id={eventId}&setup=1. Standalone use (Admin Sidebar ->
 * Food Court) never sets setup=1, so the normal Event selector still
 * works exactly as before in that case. Facilities are Venue-based, not
 * Event-based (verified against VenueFacility - no EventId relationship
 * exists), so "Save & Continue" carries the event's real venueId forward
 * as ?venueId=, plus ?eventId= only so Facilities can still show this
 * event's name in its own context bar - never as an invented backend
 * relationship.
 */
async function activateSetupMode() {
    eventPickerGroup.hidden = true;
    setupNav.hidden = false;

    let venueName = null;

    try {
        const venue = await api.get(`/api/Venues/${currentEvent.venueId}`);
        venueName = venue.name;
    } catch {
        venueName = null;
    }

    renderSetupContextBar(setupContextBar, {
        eventItem: currentEvent,
        venueName,
        stepNumber: 7,
        stepLabel: "Food Court"
    });
}


setupBackButton.addEventListener("click", () => {
    window.location.assign(`manage-parking.html?id=${currentEventId}&setup=1`);
});


setupContinueButton.addEventListener("click", () => {
    window.location.assign(
        `manage-facilities.html?venueId=${currentEvent.venueId}&setup=1&eventId=${currentEventId}`
    );
});


/* ---------------- Tabs ---------------- */

function switchTab(tabName) {
    tabButtons.forEach((button) => {
        button.classList.toggle("is-active", button.dataset.tab === tabName);
    });

    Object.entries(tabPanels).forEach(([name, panel]) => {
        panel.hidden = name !== tabName;
    });
}


tabButtons.forEach((button) => {
    button.addEventListener("click", () => switchTab(button.dataset.tab));
});


/* ---------------- Event selection (Stalls + Menu tabs share this context) ---------------- */

eventSelect.addEventListener("change", async () => {
    const value = eventSelect.value;

    if (!value) {
        currentEventId = null;
        currentEvent = null;
        stalls = [];
        itemsByStallId = new Map();

        stallsPanel.hidden = true;
        menuPanel.hidden = true;
        noEventSelectedHint.hidden = false;
        noEventSelectedHintMenu.hidden = false;

        renderSummary();
        return;
    }

    currentEventId = Number(value);
    currentEvent = events.find((eventItem) => eventItem.id === currentEventId);

    resetStallForm();
    resetItemForm();

    await loadStalls();
    await loadItemsForStalls();
});


/* ---------------- Food Stalls ---------------- */

async function loadStalls() {
    try {
        hideFeedback();
        noEventSelectedHint.hidden = true;
        noEventSelectedHintMenu.hidden = true;
        stallsPanel.hidden = true;
        menuPanel.hidden = true;
        stallsLoadingState.hidden = false;
        menuLoadingState.hidden = false;

        stalls = await api.get(`/api/events/${currentEventId}/food-stalls`);

        itemStallIdInput.innerHTML = `<option value="">Select a stall</option>`;

        stalls.forEach((stall) => {
            const option = document.createElement("option");
            option.value = stall.id;
            option.textContent = stall.name;
            itemStallIdInput.appendChild(option);
        });

        stallsLoadingState.hidden = true;
        stallsPanel.hidden = false;

        renderFoodStallList();
    } catch (error) {
        stallsLoadingState.hidden = true;
        menuLoadingState.hidden = true;

        if (error?.status === 404) {
            showFeedback("Event not found.", "error");
            return;
        }

        showFeedback(
            error.message || "Unable to load food stalls.",
            "error"
        );
    }
}


function renderFoodStallList() {
    foodStallList.innerHTML = "";

    if (!stalls.length) {
        foodStallList.innerHTML = `
            <div class="admin-empty-note">
                <p>No food stalls configured yet.</p>
                <button type="button" id="addFirstStallButton" class="btn btn-primary btn-small">
                    Add First Stall
                </button>
            </div>
        `;

        document.getElementById("addFirstStallButton")
            .addEventListener("click", focusAddStallForm);

        return;
    }

    stalls
        .slice()
        .sort((a, b) => a.name.localeCompare(b.name))
        .forEach((stall) => {
            const row = document.createElement("div");
            row.className = "food-stall-row";

            row.innerHTML = `
                <div class="food-stall-main">
                    <span class="food-stall-name">${escapeHtml(stall.name)}</span>
                    ${stall.description ? `<span class="food-stall-description">${escapeHtml(stall.description)}</span>` : ""}
                </div>

                <span class="food-stall-status-badge">${escapeHtml(stall.status)}</span>

                <div class="food-stall-actions">
                    <button type="button" class="btn btn-primary btn-small edit-stall-button" data-id="${stall.id}">
                        Edit
                    </button>
                </div>
            `;

            row.querySelector(".edit-stall-button")
                .addEventListener("click", () => startEditStall(stall.id));

            foodStallList.appendChild(row);
        });
}


function resetStallForm() {
    foodStallForm.reset();
    foodStallIdInput.value = "";

    stallFormTitle.textContent = "Add Food Stall";
    saveStallButton.textContent = "Save Stall";
    cancelStallEditButton.hidden = true;

    hideStallFormFeedback();
}


function startEditStall(stallId) {
    const stall = stalls.find((item) => item.id === stallId);

    if (!stall) {
        return;
    }

    foodStallIdInput.value = stall.id;
    stallNameInput.value = stall.name;
    stallDescriptionInput.value = stall.description || "";
    stallStatusInput.value = stall.status;

    stallFormTitle.textContent = "Edit Food Stall";
    saveStallButton.textContent = "Update Stall";
    cancelStallEditButton.hidden = false;

    hideStallFormFeedback();

    foodStallForm.scrollIntoView({ behavior: "smooth", block: "start" });
}


function focusAddStallForm() {
    resetStallForm();
    foodStallForm.scrollIntoView({ behavior: "smooth", block: "start" });
    stallNameInput.focus({ preventScroll: true });
}


async function saveStall(event) {
    event.preventDefault();

    const stallId = foodStallIdInput.value;

    const payload = {
        name: stallNameInput.value.trim(),
        description: stallDescriptionInput.value.trim() || null,
        status: stallStatusInput.value.trim()
    };

    if (!payload.name) {
        showStallFormFeedback("Stall name is required.", "error");
        return;
    }

    if (!payload.status) {
        showStallFormFeedback("Stall status is required.", "error");
        return;
    }

    try {
        setButtonLoading(saveStallButton, true, stallId ? "Updating..." : "Saving...");
        hideStallFormFeedback();

        if (stallId) {
            await api.put(`/api/events/${currentEventId}/food-stalls/${stallId}`, payload);
            showFeedback("Food stall updated successfully.", "success");
        } else {
            await api.post(`/api/events/${currentEventId}/food-stalls`, payload);
            showFeedback("Food stall created successfully.", "success");
        }

        resetStallForm();
        await loadStalls();
        await loadItemsForStalls();
    } catch (error) {
        showStallFormFeedback(
            error?.data?.message || error.message || "Unable to save food stall.",
            "error"
        );
    } finally {
        setButtonLoading(saveStallButton, false);
    }
}


function showStallFormFeedback(message, type) {
    stallFormFeedback.textContent = message;
    stallFormFeedback.className = `feedback feedback-${type}`;
    stallFormFeedback.hidden = false;
}


function hideStallFormFeedback() {
    stallFormFeedback.hidden = true;
    stallFormFeedback.textContent = "";
}


/* ---------------- Menu Items ---------------- */

async function loadItemsForStalls() {
    itemsByStallId = new Map();

    await Promise.all(stalls.map(async (stall) => {
        try {
            const items = await api.get(`/api/food-stalls/${stall.id}/items`);
            itemsByStallId.set(stall.id, items);
        } catch {
            itemsByStallId.set(stall.id, []);
        }
    }));

    menuLoadingState.hidden = true;
    menuPanel.hidden = false;

    renderFoodMenuList();
    renderSummary();
}


function renderFoodMenuList() {
    foodMenuList.innerHTML = "";

    if (!stalls.length) {
        foodMenuList.innerHTML = `
            <p class="admin-empty-note">
                No food stalls configured yet. Add a stall in the Food Stalls tab first.
            </p>
        `;
        return;
    }

    stalls
        .slice()
        .sort((a, b) => a.name.localeCompare(b.name))
        .forEach((stall) => {
            const items = itemsByStallId.get(stall.id) || [];

            const group = document.createElement("div");
            group.className = "food-menu-group";

            const heading = document.createElement("h4");
            heading.className = "food-menu-group-heading";
            heading.innerHTML = `
                ${escapeHtml(stall.name)}
                <span class="food-menu-group-count">(${items.length})</span>
            `;

            group.appendChild(heading);

            if (!items.length) {
                const empty = document.createElement("p");
                empty.className = "admin-empty-note";
                empty.textContent = "No menu items configured for this stall.";
                group.appendChild(empty);
            } else {
                items
                    .slice()
                    .sort((a, b) => a.name.localeCompare(b.name))
                    .forEach((item) => {
                        group.appendChild(buildMenuItemRow(stall.id, item));
                    });
            }

            foodMenuList.appendChild(group);
        });
}


function buildMenuItemRow(stallId, item) {
    const row = document.createElement("div");
    row.className = "food-menu-item-row";

    row.innerHTML = `
        <div class="food-menu-item-main">
            <span class="food-menu-item-name">${escapeHtml(item.name)}</span>
            ${item.description ? `<span class="food-menu-item-description">${escapeHtml(item.description)}</span>` : ""}
        </div>

        <div class="food-menu-item-price">${formatMoney(item.price)}</div>

        <span class="food-availability-badge ${item.isAvailable ? "food-availability-available" : "food-availability-unavailable"}">
            ${item.isAvailable ? "Available" : "Unavailable"}
        </span>

        <div class="food-menu-item-actions">
            <button type="button" class="btn btn-primary btn-small edit-item-button">
                Edit
            </button>
        </div>
    `;

    row.querySelector(".edit-item-button")
        .addEventListener("click", () => startEditItem(stallId, item.id));

    return row;
}


function resetItemForm() {
    foodItemForm.reset();
    foodItemIdInput.value = "";
    itemAvailableInput.value = "true";

    itemFormTitle.textContent = "Add Menu Item";
    saveItemButton.textContent = "Save Item";
    cancelItemEditButton.hidden = true;

    hideItemFormFeedback();
}


function startEditItem(stallId, itemId) {
    const items = itemsByStallId.get(stallId) || [];
    const item = items.find((entry) => entry.id === itemId);

    if (!item) {
        return;
    }

    foodItemIdInput.value = item.id;
    itemStallIdInput.value = stallId;
    itemNameInput.value = item.name;
    itemDescriptionInput.value = item.description || "";
    itemPriceInput.value = item.price;
    itemAvailableInput.value = String(item.isAvailable);

    itemFormTitle.textContent = "Edit Menu Item";
    saveItemButton.textContent = "Update Item";
    cancelItemEditButton.hidden = false;

    hideItemFormFeedback();

    foodItemForm.scrollIntoView({ behavior: "smooth", block: "start" });
}


async function saveItem(event) {
    event.preventDefault();

    const itemId = foodItemIdInput.value;
    const stallId = Number(itemStallIdInput.value);
    const price = Number(itemPriceInput.value);

    if (!stallId) {
        showItemFormFeedback("Select a food stall.", "error");
        return;
    }

    const payload = {
        name: itemNameInput.value.trim(),
        description: itemDescriptionInput.value.trim() || null,
        price,
        isAvailable: itemAvailableInput.value === "true"
    };

    if (!payload.name) {
        showItemFormFeedback("Item name is required.", "error");
        return;
    }

    if (Number.isNaN(price) || price < 0) {
        showItemFormFeedback("Price must be a non-negative number.", "error");
        return;
    }

    try {
        setButtonLoading(saveItemButton, true, itemId ? "Updating..." : "Saving...");
        hideItemFormFeedback();

        if (itemId) {
            await api.put(`/api/food-stalls/${stallId}/items/${itemId}`, payload);
            showFeedback("Menu item updated successfully.", "success");
        } else {
            await api.post(`/api/food-stalls/${stallId}/items`, payload);
            showFeedback("Menu item created successfully.", "success");
        }

        resetItemForm();
        await loadItemsForStalls();
    } catch (error) {
        showItemFormFeedback(
            error?.data?.message || error.message || "Unable to save menu item.",
            "error"
        );
    } finally {
        setButtonLoading(saveItemButton, false);
    }
}


function showItemFormFeedback(message, type) {
    itemFormFeedback.textContent = message;
    itemFormFeedback.className = `feedback feedback-${type}`;
    itemFormFeedback.hidden = false;
}


function hideItemFormFeedback() {
    itemFormFeedback.hidden = true;
    itemFormFeedback.textContent = "";
}


/* ---------------- Food Orders (global queue, real endpoints only) ---------------- */

async function loadOrdersQueue() {
    try {
        ordersPanel.hidden = true;
        ordersLoadingState.hidden = false;

        orders = await api.get("/api/food-orders");

        ordersLoadingState.hidden = true;
        ordersPanel.hidden = false;

        renderFoodOrderList();
    } catch (error) {
        ordersLoadingState.hidden = true;

        showFeedback(
            error.message || "Unable to load food orders.",
            "error"
        );
    }
}


function getFilteredOrders() {
    const term = orderSearchTerm.trim().toLowerCase();

    return orders.filter((order) => {
        const matchesStatus =
            !orderStatusFilterValue || order.status === orderStatusFilterValue;

        const matchesSearch =
            !term ||
            String(order.id).includes(term) ||
            (order.orderNumber || "").toLowerCase().includes(term) ||
            String(order.bookingId).includes(term) ||
            String(order.customerId).includes(term);

        return matchesStatus && matchesSearch;
    });
}


orderSearchInput.addEventListener("input", () => {
    orderSearchTerm = orderSearchInput.value;
    renderFoodOrderList();
});


orderStatusFilter.addEventListener("change", () => {
    orderStatusFilterValue = orderStatusFilter.value;
    renderFoodOrderList();
});


clearOrderFiltersButton.addEventListener("click", () => {
    orderSearchTerm = "";
    orderStatusFilterValue = "";
    orderSearchInput.value = "";
    orderStatusFilter.value = "";
    renderFoodOrderList();
});


function renderFoodOrderList() {
    foodOrderList.innerHTML = "";

    if (!orders.length) {
        foodOrderList.innerHTML = `
            <p class="admin-empty-note">
                No food orders found.
            </p>
        `;
        return;
    }

    const filtered = getFilteredOrders();

    if (!filtered.length) {
        foodOrderList.innerHTML = `
            <p class="admin-empty-note">
                No items match your current filters.
            </p>
        `;
        return;
    }

    filtered
        .slice()
        .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
        .forEach((order) => {
            foodOrderList.appendChild(buildOrderRow(order));
        });
}


function buildOrderRow(order) {
    const row = document.createElement("div");
    row.className = "food-order-row";

    const itemsHtml = order.items.map((item) => `
        <div class="food-order-item-line">
            <span>${escapeHtml(item.foodItemName)} &times; ${item.quantity}</span>
            <span>${formatMoney(item.lineTotal)}</span>
        </div>
    `).join("");

    const nextStatuses = STATUS_TRANSITIONS[order.status] || [];

    const actionsHtml = nextStatuses.map((nextStatus) => `
        <button type="button" class="btn ${nextStatus === "Cancelled" ? "btn-secondary" : "btn-primary"} btn-small order-action-button" data-order-id="${order.id}" data-next-status="${nextStatus}">
            ${escapeHtml(STATUS_ACTION_LABEL[nextStatus] || nextStatus)}
        </button>
    `).join("");

    row.innerHTML = `
        <div class="food-order-header">
            <span class="food-order-number">${escapeHtml(order.orderNumber || `Order #${order.id}`)}</span>
            <span class="food-order-status-badge food-order-status-${order.status.toLowerCase()}">
                ${escapeHtml(STATUS_DISPLAY_LABEL[order.status] || order.status)}
            </span>
            <span class="food-order-time">${formatDateTime(order.createdAt)}</span>
        </div>

        <div class="food-order-meta">
            <span>Customer #${order.customerId}</span>
            <span>Booking #${order.bookingId}</span>
            <span>${escapeHtml(resolveStallLabel(order.foodStallId))}</span>
            <span>Pickup: ${formatDateTime(order.pickupTime)}</span>
        </div>

        <div class="food-order-items">
            ${itemsHtml}
        </div>

        <div class="food-order-footer">
            <span class="food-order-total">Total: ${formatMoney(order.totalAmount)}</span>
            <div class="food-order-actions">
                ${actionsHtml}
            </div>
        </div>
    `;

    row.querySelectorAll(".order-action-button").forEach((button) => {
        button.addEventListener("click", () =>
            updateOrderStatus(
                Number(button.dataset.orderId),
                button.dataset.nextStatus,
                button
            ));
    });

    return row;
}


function resolveStallLabel(foodStallId) {
    const stall = stalls.find((item) => item.id === foodStallId);
    return stall ? stall.name : `Stall #${foodStallId}`;
}


async function updateOrderStatus(orderId, nextStatusName, triggerButton) {
    const statusValue = FOOD_ORDER_STATUS[nextStatusName];

    if (!statusValue) {
        return;
    }

    try {
        setButtonLoading(triggerButton, true, "Updating...");
        hideOrderStatusFeedback();

        await api.put(`/api/food-orders/${orderId}/status`, {
            status: statusValue
        });

        showOrderStatusFeedback(
            `Order updated to ${STATUS_DISPLAY_LABEL[nextStatusName] || nextStatusName}.`,
            "success"
        );

        await loadOrdersQueue();
        renderSummary();
    } catch (error) {
        showOrderStatusFeedback(
            error?.data?.message || error.message || "Unable to update order status.",
            "error"
        );
        setButtonLoading(triggerButton, false);
    }
}


function showOrderStatusFeedback(message, type) {
    orderStatusFeedback.textContent = message;
    orderStatusFeedback.className = `feedback feedback-${type}`;
    orderStatusFeedback.hidden = false;
}


function hideOrderStatusFeedback() {
    orderStatusFeedback.hidden = true;
    orderStatusFeedback.textContent = "";
}


/* ---------------- Real, client-derived summary (no fake values) ---------------- */

function renderSummary() {
    const allItems = Array.from(itemsByStallId.values()).flat();

    statTotalStalls.textContent = stalls.length;

    statActiveStalls.textContent = stalls.filter(
        (stall) => (stall.status || "").trim().toLowerCase() === "active"
    ).length;

    statMenuItems.textContent = allItems.length;

    statAvailableItems.textContent = allItems.filter(
        (item) => item.isAvailable
    ).length;

    statPendingOrders.textContent = orders.filter(
        (order) => order.status === "Pending"
    ).length;
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


foodStallForm.addEventListener("submit", saveStall);
foodItemForm.addEventListener("submit", saveItem);

cancelStallEditButton.addEventListener("click", () => {
    resetStallForm();
    hideFeedback();
});

cancelItemEditButton.addEventListener("click", () => {
    resetItemForm();
    hideFeedback();
});
