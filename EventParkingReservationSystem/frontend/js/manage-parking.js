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
 * ParkingSlotStatus mirrors EventParking.Models.Enums.ParkingSlotStatus
 * exactly (Available = 1, Held = 2, Reserved = 3, Unavailable = 4,
 * Occupied = 5). CreateParkingSlotRequest/UpdateParkingSlotRequest.Status
 * has no JsonStringEnumConverter, so it must be sent as this numeric
 * value - the response Status field, however, is already a string
 * (ParkingSlotService.MapToResponse calls .ToString()). Held and Reserved
 * are left out of the manual status <select> because they are set by the
 * booking/parking-reservation flow, not by an admin directly - and the
 * backend rejects editing a slot that is already Held or Reserved.
 */
const PARKING_SLOT_STATUS = {
    AVAILABLE: 1,
    HELD: 2,
    RESERVED: 3,
    UNAVAILABLE: 4,
    OCCUPIED: 5
};

const PROTECTED_STATUS_NAMES = new Set(["Held", "Reserved"]);
const STATUS_NAMES = ["Available", "Held", "Reserved", "Unavailable", "Occupied"];

const UNASSIGNED_ZONE_LABEL = "Unassigned Zone";


const adminShell = document.getElementById("adminShell");
const parkingAdminBody = document.getElementById("parkingAdminBody");
const setupContextBar = document.getElementById("setupContextBar");
const eventPickerGroup = document.getElementById("eventPickerGroup");
const setupNav = document.getElementById("setupNav");
const setupBackButton = document.getElementById("setupBackButton");
const setupContinueButton = document.getElementById("setupContinueButton");

const eventSelect = document.getElementById("eventSelect");
const noEventSelectedHint = document.getElementById("noEventSelectedHint");
const parkingLoadingState = document.getElementById("parkingLoadingState");
const parkingManagementPanel = document.getElementById("parkingManagementPanel");

const statTotalSlots = document.getElementById("statTotalSlots");
const statAvailableSlots = document.getElementById("statAvailableSlots");
const statHeldSlots = document.getElementById("statHeldSlots");
const statReservedSlots = document.getElementById("statReservedSlots");
const statUnavailableSlots = document.getElementById("statUnavailableSlots");
const statOccupiedSlots = document.getElementById("statOccupiedSlots");
const statDefaultFee = document.getElementById("statDefaultFee");

const bulkParkingForm = document.getElementById("bulkParkingForm");
const bulkZoneInput = document.getElementById("bulkZone");
const bulkCapacityInput = document.getElementById("bulkCapacity");
const bulkDefaultFeeInput = document.getElementById("bulkDefaultFee");
const bulkParkingFeedback = document.getElementById("bulkParkingFeedback");
const previewBulkButton = document.getElementById("previewBulkButton");

const bulkPreviewPanel = document.getElementById("bulkPreviewPanel");
const bulkPreviewZone = document.getElementById("bulkPreviewZone");
const bulkPreviewCount = document.getElementById("bulkPreviewCount");
const bulkPreviewRange = document.getElementById("bulkPreviewRange");
const bulkPreviewFee = document.getElementById("bulkPreviewFee");
const confirmBulkButton = document.getElementById("confirmBulkButton");
const cancelBulkButton = document.getElementById("cancelBulkButton");
const bulkProgressFeedback = document.getElementById("bulkProgressFeedback");

const parkingSlotForm = document.getElementById("parkingSlotForm");
const parkingSlotIdInput = document.getElementById("parkingSlotId");
const slotNumberInput = document.getElementById("slotNumber");
const slotZoneInput = document.getElementById("slotZone");
const slotFeeOverrideInput = document.getElementById("slotFeeOverride");
const slotStatusInput = document.getElementById("slotStatus");
const slotFormTitle = document.getElementById("slotFormTitle");
const slotFormFeedback = document.getElementById("slotFormFeedback");
const saveSlotButton = document.getElementById("saveSlotButton");
const cancelSlotEditButton = document.getElementById("cancelSlotEditButton");

const parkingSearchInput = document.getElementById("parkingSearchInput");
const parkingStatusFilter = document.getElementById("parkingStatusFilter");
const clearParkingFiltersButton = document.getElementById("clearParkingFiltersButton");

const parkingSlotList = document.getElementById("parkingSlotList");


let events = [];
let currentEventId = null;
let currentEvent = null;
let slots = [];

let searchTerm = "";
let statusFilter = "";

let pendingBulkPlan = null;

/*
 * Frontend-only safety cap on a single bulk generation run - the backend
 * has no bulk endpoint or documented limit, this purely exists so an admin
 * mistake (e.g. typing 5000) can't fire thousands of sequential POST
 * requests. Matches the max="500" on #bulkCapacity.
 */
const BULK_CAPACITY_SAFETY_MAX = 500;

const setupParams = getSetupParams();


document.addEventListener("DOMContentLoaded", async () => {
    if (!requireAdministrator()) {
        return;
    }

    renderAdminSidebar("parking");
    setupAdminMobileMenu();

    adminShell.hidden = false;
    parkingAdminBody.hidden = false;

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
            noEventSelectedHint.hidden = true;

            if (setupParams.isSetup) {
                await activateSetupMode();
            }

            await loadSlotsForEvent();
        }
    } catch (error) {
        showFeedback(
            error.message || "Unable to load events.",
            "error"
        );
    }
}


/*
 * Continuous Event Setup Flow (Corrections 8-13): only active via
 * manage-parking.html?id={eventId}&setup=1. Standalone use (Admin
 * Sidebar -> Parking) never sets setup=1, so the normal Event selector
 * still works exactly as before in that case.
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
        stepNumber: 6,
        stepLabel: "Parking"
    });
}


setupBackButton.addEventListener("click", () => {
    window.location.assign(`manage-seats.html?id=${currentEventId}&setup=1`);
});


setupContinueButton.addEventListener("click", () => {
    window.location.assign(`manage-food.html?id=${currentEventId}&setup=1`);
});


eventSelect.addEventListener("change", async () => {
    const value = eventSelect.value;

    if (!value) {
        parkingManagementPanel.hidden = true;
        noEventSelectedHint.hidden = false;
        currentEventId = null;
        currentEvent = null;
        slots = [];
        return;
    }

    currentEventId = Number(value);
    currentEvent = events.find((eventItem) => eventItem.id === currentEventId);
    noEventSelectedHint.hidden = true;
    resetSlotForm();
    resetBulkForm();
    resetFilters();
    await loadSlotsForEvent();
});


function resetBulkForm() {
    bulkParkingForm.reset();
    pendingBulkPlan = null;
    bulkPreviewPanel.hidden = true;
    confirmBulkButton.hidden = false;
    hideBulkFeedback();
    hideBulkProgressFeedback();
}


async function loadSlotsForEvent() {
    try {
        hideFeedback();
        parkingManagementPanel.hidden = true;
        parkingLoadingState.hidden = false;

        slots = await api.get(`/api/events/${currentEventId}/parking-slots`);

        parkingLoadingState.hidden = true;
        parkingManagementPanel.hidden = false;

        renderSummary();
        renderParkingSlotList();
    } catch (error) {
        parkingLoadingState.hidden = true;

        if (error?.status === 404) {
            showFeedback("Event not found.", "error");
            return;
        }

        showFeedback(
            error.message || "Unable to load parking slots.",
            "error"
        );
    }
}


/* ---------------- Status summary (client-derived, no fake values) ---------------- */

function renderSummary() {
    const countByStatus = STATUS_NAMES.reduce((counts, name) => {
        counts[name] = slots.filter((slot) => slot.status === name).length;
        return counts;
    }, {});

    statTotalSlots.textContent = slots.length;
    statAvailableSlots.textContent = countByStatus.Available;
    statHeldSlots.textContent = countByStatus.Held;
    statReservedSlots.textContent = countByStatus.Reserved;
    statUnavailableSlots.textContent = countByStatus.Unavailable;
    statOccupiedSlots.textContent = countByStatus.Occupied;
    statDefaultFee.textContent = formatMoney(currentEvent ? currentEvent.parkingFee : 0);
}


/* ---------------- Search + Filter (client-side only, no new API calls) ---------------- */

function getFilteredSlots() {
    const term = searchTerm.trim().toLowerCase();

    return slots.filter((slot) => {
        const matchesStatus = !statusFilter || slot.status === statusFilter;

        const matchesSearch =
            !term ||
            slot.slotNumber.toLowerCase().includes(term) ||
            (slot.zone || "").toLowerCase().includes(term);

        return matchesStatus && matchesSearch;
    });
}


function resetFilters() {
    searchTerm = "";
    statusFilter = "";
    parkingSearchInput.value = "";
    parkingStatusFilter.value = "";
}


parkingSearchInput.addEventListener("input", () => {
    searchTerm = parkingSearchInput.value;
    renderParkingSlotList();
});


parkingStatusFilter.addEventListener("change", () => {
    statusFilter = parkingStatusFilter.value;
    renderParkingSlotList();
});


clearParkingFiltersButton.addEventListener("click", () => {
    resetFilters();
    renderParkingSlotList();
});


/* ---------------- Add / Edit form ---------------- */

function resetSlotForm() {
    parkingSlotForm.reset();
    parkingSlotIdInput.value = "";
    slotStatusInput.value = String(PARKING_SLOT_STATUS.AVAILABLE);

    slotFormTitle.textContent = "Add Parking Slot";
    saveSlotButton.textContent = "Save Slot";
    cancelSlotEditButton.hidden = true;

    hideSlotFormFeedback();
}


function startEditSlot(slotId) {
    const slot = slots.find((item) => item.id === slotId);

    if (!slot) {
        return;
    }

    if (PROTECTED_STATUS_NAMES.has(slot.status)) {
        return;
    }

    parkingSlotIdInput.value = slot.id;
    slotNumberInput.value = slot.slotNumber;
    slotZoneInput.value = slot.zone || "";
    slotFeeOverrideInput.value = slot.feeOverride ?? "";
    slotStatusInput.value = String(statusNameToValue(slot.status));

    slotFormTitle.textContent = "Edit Parking Slot";
    saveSlotButton.textContent = "Update Slot";
    cancelSlotEditButton.hidden = false;

    hideSlotFormFeedback();

    parkingSlotForm.scrollIntoView({ behavior: "smooth", block: "start" });
}


function focusAddSlotForm() {
    resetSlotForm();
    parkingSlotForm.scrollIntoView({ behavior: "smooth", block: "start" });
    slotNumberInput.focus({ preventScroll: true });
}


async function saveSlot(event) {
    event.preventDefault();

    const slotId = parkingSlotIdInput.value;

    const feeOverrideRaw = slotFeeOverrideInput.value;
    const feeOverride = feeOverrideRaw === "" ? null : Number(feeOverrideRaw);

    const payload = {
        slotNumber: slotNumberInput.value.trim(),
        zone: slotZoneInput.value.trim() || null,
        feeOverride,
        status: Number(slotStatusInput.value)
    };

    if (!payload.slotNumber) {
        showSlotFormFeedback("Slot number is required.", "error");
        return;
    }

    if (feeOverride !== null && (Number.isNaN(feeOverride) || feeOverride < 0)) {
        showSlotFormFeedback("Fee override must be a non-negative number.", "error");
        return;
    }

    try {
        setButtonLoading(saveSlotButton, true, slotId ? "Updating..." : "Saving...");
        hideSlotFormFeedback();

        if (slotId) {
            await api.put(`/api/events/${currentEventId}/parking-slots/${slotId}`, payload);
            showFeedback("Parking slot updated successfully.", "success");
        } else {
            await api.post(`/api/events/${currentEventId}/parking-slots`, payload);
            showFeedback("Parking slot created successfully.", "success");
        }

        resetSlotForm();
        await loadSlotsForEvent();
    } catch (error) {
        showSlotFormFeedback(
            error?.data?.message || error.message || "Unable to save parking slot.",
            "error"
        );
    } finally {
        setButtonLoading(saveSlotButton, false);
    }
}


async function deleteSlot(slotId) {
    const slot = slots.find((item) => item.id === slotId);

    if (!slot) {
        return;
    }

    const confirmed = window.confirm(`Delete parking slot "${slot.slotNumber}"?`);

    if (!confirmed) {
        return;
    }

    try {
        hideFeedback();

        await api.delete(`/api/events/${currentEventId}/parking-slots/${slotId}`);

        showFeedback("Parking slot deleted successfully.", "success");
        await loadSlotsForEvent();
    } catch (error) {
        showFeedback(
            error?.data?.message || error.message || "Unable to delete parking slot.",
            "error"
        );
    }
}


/* ---------------- Bulk Generate Parking Slots ---------------- */

/*
 * Slot numbers are generated as {ZONE}{01, 02, ...} directly from the Zone
 * value typed in (uppercased, non-alphanumeric characters stripped) - the
 * real project seed data uses a free-text Zone ("Zone A") separate from a
 * differently-prefixed SlotNumber ("P01"), so there is no single existing
 * convention to derive a prefix from; using the admin's own Zone value
 * keeps this deterministic and, per the correction brief's own worked
 * example (Zone "B" -> B01..B20), matches the intended behavior when a
 * short zone code is used. The generated list is always shown in the
 * preview step before any request is sent, so the admin sees exactly what
 * will be created.
 */
function buildBulkSlotNumbers(zone, capacity) {
    const prefix = zone.toUpperCase().replace(/[^A-Z0-9]/g, "") || "ZONE";
    const padLength = Math.max(2, String(capacity).length);

    const numbers = [];

    for (let i = 1; i <= capacity; i++) {
        numbers.push(`${prefix}${String(i).padStart(padLength, "0")}`);
    }

    return numbers;
}


bulkParkingForm.addEventListener("submit", (event) => {
    event.preventDefault();

    hideBulkFeedback();
    bulkPreviewPanel.hidden = true;
    pendingBulkPlan = null;

    const zone = bulkZoneInput.value.trim();
    const capacity = Number(bulkCapacityInput.value);
    const feeRaw = bulkDefaultFeeInput.value;
    const defaultFee = feeRaw === "" ? null : Number(feeRaw);

    if (!zone) {
        showBulkFeedback("Zone is required.", "error");
        return;
    }

    if (!Number.isInteger(capacity) || capacity < 1) {
        showBulkFeedback("Slot capacity must be a whole number of at least 1.", "error");
        return;
    }

    if (capacity > BULK_CAPACITY_SAFETY_MAX) {
        showBulkFeedback(
            `Slot capacity cannot exceed ${BULK_CAPACITY_SAFETY_MAX} in a single bulk generation run.`,
            "error"
        );
        return;
    }

    if (defaultFee !== null && (Number.isNaN(defaultFee) || defaultFee < 0)) {
        showBulkFeedback("Default fee must be a non-negative number.", "error");
        return;
    }

    const slotNumbers = buildBulkSlotNumbers(zone, capacity);

    const existingNumbers = new Set(slots.map((slot) => slot.slotNumber));
    const duplicates = slotNumbers.filter((number) => existingNumbers.has(number));

    if (duplicates.length) {
        showBulkFeedback(
            `Zone ${zone} already contains one or more of these slot numbers ` +
            `(${duplicates.slice(0, 5).join(", ")}${duplicates.length > 5 ? ", ..." : ""}). ` +
            "Choose another zone or adjust the slot capacity.",
            "error"
        );
        return;
    }

    pendingBulkPlan = { zone, capacity, defaultFee, slotNumbers };

    confirmBulkButton.hidden = false;
    confirmBulkButton.textContent = "Generate Slots";

    bulkPreviewZone.textContent = zone;
    bulkPreviewCount.textContent = capacity;
    bulkPreviewRange.textContent =
        capacity === 1
            ? slotNumbers[0]
            : `${slotNumbers[0]} – ${slotNumbers[capacity - 1]}`;
    bulkPreviewFee.textContent =
        defaultFee === null ? "Event default" : formatMoney(defaultFee);

    bulkPreviewPanel.hidden = false;
    bulkPreviewPanel.scrollIntoView({ behavior: "smooth", block: "nearest" });
});


cancelBulkButton.addEventListener("click", () => {
    pendingBulkPlan = null;
    bulkPreviewPanel.hidden = true;
    confirmBulkButton.hidden = false;
    hideBulkFeedback();
    hideBulkProgressFeedback();
});


confirmBulkButton.addEventListener("click", async () => {
    if (!pendingBulkPlan) {
        return;
    }

    const { zone, defaultFee, slotNumbers } = pendingBulkPlan;
    const total = slotNumbers.length;
    let createdCount = 0;

    confirmBulkButton.disabled = true;
    cancelBulkButton.disabled = true;
    hideBulkProgressFeedback();

    for (const slotNumber of slotNumbers) {
        confirmBulkButton.textContent = `Generating ${createdCount + 1} of ${total}...`;

        try {
            await api.post(`/api/events/${currentEventId}/parking-slots`, {
                slotNumber,
                zone,
                feeOverride: defaultFee,
                status: PARKING_SLOT_STATUS.AVAILABLE
            });

            createdCount++;
        } catch (error) {
            /*
             * The plan is invalidated (not just re-enabled) - some of its
             * slot numbers now exist for real, so blindly re-running the
             * same plan would immediately duplicate-fail on those. The
             * panel stays open to show the stop message; Confirm is
             * hidden so there is nothing stale left to click, and Cancel
             * dismisses it. A fresh Preview run re-checks against the
             * just-refreshed real slot list.
             */
            pendingBulkPlan = null;
            confirmBulkButton.hidden = true;
            cancelBulkButton.disabled = false;

            await loadSlotsForEvent();

            showBulkProgressFeedback(
                `${createdCount} of ${total} parking slots were created. Creation stopped because ` +
                `${slotNumber} could not be created: ` +
                (error?.data?.message || error.message || "Unknown error."),
                "error"
            );

            return;
        }
    }

    pendingBulkPlan = null;
    bulkPreviewPanel.hidden = true;
    confirmBulkButton.disabled = false;
    cancelBulkButton.disabled = false;
    confirmBulkButton.textContent = "Generate Slots";

    bulkParkingForm.reset();

    await loadSlotsForEvent();

    showFeedback(`${total} parking slots created successfully.`, "success");
});


function showBulkFeedback(message, type) {
    bulkParkingFeedback.textContent = message;
    bulkParkingFeedback.className = `feedback feedback-${type}`;
    bulkParkingFeedback.hidden = false;
}


function hideBulkFeedback() {
    bulkParkingFeedback.hidden = true;
    bulkParkingFeedback.textContent = "";
}


function showBulkProgressFeedback(message, type) {
    bulkProgressFeedback.textContent = message;
    bulkProgressFeedback.className = `feedback feedback-${type}`;
    bulkProgressFeedback.hidden = false;
}


function hideBulkProgressFeedback() {
    bulkProgressFeedback.hidden = true;
    bulkProgressFeedback.textContent = "";
}


/* ---------------- Slot list: zone-grouped, search/filter applied client-side ---------------- */

function renderParkingSlotList() {
    parkingSlotList.innerHTML = "";

    if (!slots.length) {
        const empty = document.createElement("div");
        empty.className = "admin-empty-note";
        empty.innerHTML = `
            <p>No parking slots configured for this event yet.</p>
            <button type="button" id="addFirstSlotButton" class="btn btn-primary btn-small">
                Add First Parking Slot
            </button>
        `;

        parkingSlotList.appendChild(empty);

        document.getElementById("addFirstSlotButton")
            .addEventListener("click", focusAddSlotForm);

        return;
    }

    const filtered = getFilteredSlots();

    if (!filtered.length) {
        parkingSlotList.innerHTML = `
            <p class="admin-empty-note">
                No parking slots match your search/filter.
            </p>
        `;
        return;
    }

    groupSlotsByZone(filtered).forEach((group) => {
        const section = document.createElement("div");
        section.className = "parking-zone-group";

        const heading = document.createElement("h4");
        heading.className = "parking-zone-heading";
        heading.innerHTML = `
            ${escapeHtml(group.zoneName)}
            <span class="parking-zone-count">(${group.slots.length})</span>
        `;

        section.appendChild(heading);

        group.slots.forEach((slot) => {
            section.appendChild(buildSlotRow(slot));
        });

        parkingSlotList.appendChild(section);
    });
}


/*
 * Groups slots by their real Zone value only - never alters or invents
 * backend data. A blank/null zone is grouped under a friendly
 * "Unassigned Zone" label for display purposes only.
 */
function groupSlotsByZone(slotList) {
    const zoneMap = new Map();

    slotList.forEach((slot) => {
        const zoneName = slot.zone && slot.zone.trim()
            ? slot.zone.trim()
            : UNASSIGNED_ZONE_LABEL;

        if (!zoneMap.has(zoneName)) {
            zoneMap.set(zoneName, []);
        }

        zoneMap.get(zoneName).push(slot);
    });

    const zoneNames = Array.from(zoneMap.keys()).sort((a, b) => {
        if (a === UNASSIGNED_ZONE_LABEL) {
            return 1;
        }

        if (b === UNASSIGNED_ZONE_LABEL) {
            return -1;
        }

        return a.localeCompare(b);
    });

    return zoneNames.map((zoneName) => ({
        zoneName,
        slots: zoneMap.get(zoneName)
            .slice()
            .sort((a, b) => a.slotNumber.localeCompare(b.slotNumber, undefined, { numeric: true }))
    }));
}


function buildSlotRow(slot) {
    const isProtected = PROTECTED_STATUS_NAMES.has(slot.status);
    const hasOverride = slot.feeOverride !== null && slot.feeOverride !== undefined;

    const row = document.createElement("div");
    row.className = "parking-slot-row";

    row.innerHTML = `
        <div class="parking-slot-main">
            <span class="parking-slot-number">${escapeHtml(slot.slotNumber)}</span>
        </div>

        <div class="parking-slot-fee">
            ${formatMoney(slot.effectiveFee)}
        </div>

        <div class="parking-slot-badges">
            <span class="parking-status-badge parking-status-${slot.status.toLowerCase()}">
                ${escapeHtml(slot.status)}
            </span>
            ${hasOverride ? '<span class="parking-fee-override-badge">Override active</span>' : ""}
        </div>

        <div class="parking-slot-actions">
            <button type="button" class="btn btn-primary btn-small edit-slot-button" data-id="${slot.id}" ${isProtected ? "disabled" : ""}>
                Edit
            </button>
            <button type="button" class="btn btn-secondary btn-small delete-slot-button" data-id="${slot.id}" ${isProtected ? "disabled" : ""}>
                Delete
            </button>
        </div>

        ${isProtected ? `<p class="parking-slot-locked-note">This slot is ${slot.status.toLowerCase()} and is managed automatically by the booking system.</p>` : ""}
    `;

    row.querySelector(".edit-slot-button")
        .addEventListener("click", () => startEditSlot(slot.id));

    row.querySelector(".delete-slot-button")
        .addEventListener("click", () => deleteSlot(slot.id));

    return row;
}


function statusNameToValue(statusName) {
    const entry = Object.entries(PARKING_SLOT_STATUS).find(
        ([key]) => key === statusName.toUpperCase()
    );

    return entry ? entry[1] : PARKING_SLOT_STATUS.AVAILABLE;
}


function showSlotFormFeedback(message, type) {
    slotFormFeedback.textContent = message;
    slotFormFeedback.className = `feedback feedback-${type}`;
    slotFormFeedback.hidden = false;
}


function hideSlotFormFeedback() {
    slotFormFeedback.hidden = true;
    slotFormFeedback.textContent = "";
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


parkingSlotForm.addEventListener("submit", saveSlot);

cancelSlotEditButton.addEventListener("click", () => {
    resetSlotForm();
    hideFeedback();
});
