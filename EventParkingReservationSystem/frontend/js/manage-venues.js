import { api } from "./api.js";

import {
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";

import {
    requireAdministrator,
    renderAdminSidebar,
    setupAdminMobileMenu
} from "./admin-ui.js";


/*
 * VenueDto (VenuesController.MapToDto) only ever returns Id, Name,
 * Address, TotalCapacity, CreatedAt, UpdatedAt - and
 * CreateVenueRequest/UpdateVenueRequest only accept Name (required,
 * <=150), Address (required, <=500) and TotalCapacity (int, >=1). No
 * other field exists on the backend Venue contract, so none is rendered
 * here (no image/map/lat-long/contact/phone/description).
 *
 * DELETE /api/Venues/{id} is real and Administrator-only. It fails
 * (400) when the venue has any event with StartDateTime >= now
 * (VenueService.DeleteAsync) - that real backend message is surfaced
 * as-is rather than rewritten, since it is already accurate and clear.
 */

const adminShell = document.getElementById("adminShell");
const venueAdminBody = document.getElementById("venueAdminBody");
const venuesLoadingState = document.getElementById("venuesLoadingState");
const venuesLoadError = document.getElementById("venuesLoadError");
const venuesLoadErrorText = document.getElementById("venuesLoadErrorText");
const retryLoadVenuesButton = document.getElementById("retryLoadVenuesButton");

const venueForm = document.getElementById("venueForm");
const venueIdInput = document.getElementById("venueId");
const venueNameInput = document.getElementById("venueName");
const venueAddressInput = document.getElementById("venueAddress");
const venueCapacityInput = document.getElementById("venueCapacity");
const venueFormTitle = document.getElementById("venueFormTitle");
const venueFormFeedback = document.getElementById("venueFormFeedback");
const saveVenueButton = document.getElementById("saveVenueButton");
const cancelVenueEditButton = document.getElementById("cancelVenueEditButton");

const venueSearchInput = document.getElementById("venueSearchInput");
const venueList = document.getElementById("venueList");


let venues = [];
let searchTerm = "";


document.addEventListener("DOMContentLoaded", async () => {
    if (!requireAdministrator()) {
        return;
    }

    renderAdminSidebar("venues");
    setupAdminMobileMenu();

    adminShell.hidden = false;

    await loadVenues();
});


async function loadVenues() {
    try {
        hideFeedback();
        venuesLoadError.hidden = true;
        venueAdminBody.hidden = true;
        venuesLoadingState.hidden = false;

        venues = await api.get("/api/Venues");

        venuesLoadingState.hidden = true;
        venueAdminBody.hidden = false;

        renderVenueList();
    } catch (error) {
        venuesLoadingState.hidden = true;

        venuesLoadErrorText.textContent =
            error?.data?.message || error.message || "Unable to load venues.";
        venuesLoadError.hidden = false;
    }
}


retryLoadVenuesButton.addEventListener("click", loadVenues);


/* ---------------- Search (client-side only, no new API calls) ---------------- */

function getFilteredVenues() {
    const term = searchTerm.trim().toLowerCase();

    if (!term) {
        return venues;
    }

    return venues.filter((venue) =>
        venue.name.toLowerCase().includes(term) ||
        venue.address.toLowerCase().includes(term));
}


venueSearchInput.addEventListener("input", () => {
    searchTerm = venueSearchInput.value;
    renderVenueList();
});


/* ---------------- Venue list ---------------- */

function renderVenueList() {
    venueList.innerHTML = "";

    if (!venues.length) {
        venueList.innerHTML = `
            <div class="admin-empty-note">
                <p><strong>No venues yet.</strong> Create your first venue before setting up an event.</p>
                <button type="button" id="addFirstVenueButton" class="btn btn-primary btn-small">
                    + Create Venue
                </button>
            </div>
        `;

        document.getElementById("addFirstVenueButton")
            .addEventListener("click", focusAddVenueForm);

        return;
    }

    const filtered = getFilteredVenues();

    if (!filtered.length) {
        venueList.innerHTML = `
            <p class="admin-empty-note">
                No venues match your search.
            </p>
        `;
        return;
    }

    filtered
        .slice()
        .sort((a, b) => a.name.localeCompare(b.name))
        .forEach((venue) => {
            venueList.appendChild(buildVenueRow(venue));
        });
}


function buildVenueRow(venue) {
    const row = document.createElement("div");
    row.className = "venue-row";

    row.innerHTML = `
        <div class="venue-main">
            <span class="venue-name">${escapeHtml(venue.name)}</span>
            <span class="venue-address">${escapeHtml(venue.address)}</span>
        </div>

        <span class="venue-capacity-badge">Capacity: ${venue.totalCapacity}</span>

        <div class="venue-actions">
            <button type="button" class="btn btn-primary btn-small edit-venue-button" data-id="${venue.id}">
                Edit
            </button>
            <button type="button" class="btn btn-secondary btn-small delete-venue-button" data-id="${venue.id}">
                Delete
            </button>
        </div>
    `;

    row.querySelector(".edit-venue-button")
        .addEventListener("click", () => startEditVenue(venue.id));

    row.querySelector(".delete-venue-button")
        .addEventListener("click", () => deleteVenue(venue.id));

    return row;
}


/* ---------------- Add / Edit form ---------------- */

function resetVenueForm() {
    venueForm.reset();
    venueIdInput.value = "";

    venueFormTitle.textContent = "Add Venue";
    saveVenueButton.textContent = "Save Venue";
    cancelVenueEditButton.hidden = true;

    hideVenueFormFeedback();
}


function startEditVenue(venueId) {
    const venue = venues.find((item) => item.id === venueId);

    if (!venue) {
        return;
    }

    venueIdInput.value = venue.id;
    venueNameInput.value = venue.name;
    venueAddressInput.value = venue.address;
    venueCapacityInput.value = venue.totalCapacity;

    venueFormTitle.textContent = "Edit Venue";
    saveVenueButton.textContent = "Update Venue";
    cancelVenueEditButton.hidden = false;

    hideVenueFormFeedback();

    venueForm.scrollIntoView({ behavior: "smooth", block: "start" });
}


function focusAddVenueForm() {
    resetVenueForm();
    venueForm.scrollIntoView({ behavior: "smooth", block: "start" });
    venueNameInput.focus({ preventScroll: true });
}


async function saveVenue(event) {
    event.preventDefault();

    const venueId = venueIdInput.value;
    const capacity = Number(venueCapacityInput.value);

    const payload = {
        name: venueNameInput.value.trim(),
        address: venueAddressInput.value.trim(),
        totalCapacity: capacity
    };

    if (!payload.name) {
        showVenueFormFeedback("Venue name is required.", "error");
        return;
    }

    if (!payload.address) {
        showVenueFormFeedback("Address is required.", "error");
        return;
    }

    if (!Number.isFinite(capacity) || capacity < 1) {
        showVenueFormFeedback("Total capacity must be a positive number.", "error");
        return;
    }

    try {
        setButtonLoading(saveVenueButton, true, venueId ? "Updating..." : "Saving...");
        hideVenueFormFeedback();

        if (venueId) {
            await api.put(`/api/Venues/${venueId}`, payload);
            showFeedback("Venue updated successfully.", "success");
        } else {
            await api.post("/api/Venues", payload);
            showFeedback("Venue created successfully.", "success");
        }

        resetVenueForm();
        await loadVenues();
    } catch (error) {
        showVenueFormFeedback(
            error?.data?.message || error.message || "Unable to save venue.",
            "error"
        );
    } finally {
        setButtonLoading(saveVenueButton, false);
    }
}


async function deleteVenue(venueId) {
    const venue = venues.find((item) => item.id === venueId);

    if (!venue) {
        return;
    }

    const confirmed = window.confirm(
        `Delete venue "${venue.name}"? This action cannot be undone.`
    );

    if (!confirmed) {
        return;
    }

    try {
        hideFeedback();

        await api.delete(`/api/Venues/${venueId}`);

        showFeedback("Venue deleted successfully.", "success");
        await loadVenues();
    } catch (error) {
        showFeedback(
            error?.data?.message || error.message || "Unable to delete venue.",
            "error"
        );
    }
}


function showVenueFormFeedback(message, type) {
    venueFormFeedback.textContent = message;
    venueFormFeedback.className = `feedback feedback-${type}`;
    venueFormFeedback.hidden = false;
}


function hideVenueFormFeedback() {
    venueFormFeedback.hidden = true;
    venueFormFeedback.textContent = "";
}


function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}


venueForm.addEventListener("submit", saveVenue);

cancelVenueEditButton.addEventListener("click", () => {
    resetVenueForm();
    hideFeedback();
});
