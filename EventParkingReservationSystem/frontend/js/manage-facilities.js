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
 * FacilityType/FacilityStatus mirror EventParking.Models.Enums exactly.
 * FacilityType: Washroom=1, FirstAid=2, PrayerRoom=3, Atm=4,
 * InformationDesk=5, Exit=6. FacilityStatus: Open=1, Closed=2,
 * UnderMaintenance=3. CreateVenueFacilityRequest/UpdateVenueFacilityRequest
 * have no JsonStringEnumConverter, so both must be sent as these numeric
 * values - VenueFacilityDto.FacilityType/Status, however, are already
 * strings (FacilitiesController.MapToDto calls .ToString() on each).
 */
const FACILITY_TYPE = {
    Washroom: 1,
    FirstAid: 2,
    PrayerRoom: 3,
    Atm: 4,
    InformationDesk: 5,
    Exit: 6
};

const FACILITY_TYPE_LABEL = {
    Washroom: "Washroom",
    FirstAid: "First Aid",
    PrayerRoom: "Prayer Room",
    Atm: "ATM",
    InformationDesk: "Information Desk",
    Exit: "Exit"
};

const FACILITY_STATUS = {
    Open: 1,
    Closed: 2,
    UnderMaintenance: 3
};

const FACILITY_STATUS_LABEL = {
    Open: "Open",
    Closed: "Closed",
    UnderMaintenance: "Under Maintenance"
};


const adminShell = document.getElementById("adminShell");
const facilityAdminBody = document.getElementById("facilityAdminBody");

const venueSelect = document.getElementById("venueSelect");
const noVenueSelectedHint = document.getElementById("noVenueSelectedHint");
const facilitiesLoadingState = document.getElementById("facilitiesLoadingState");
const facilityManagementPanel = document.getElementById("facilityManagementPanel");

const statTotalFacilities = document.getElementById("statTotalFacilities");
const statOpenFacilities = document.getElementById("statOpenFacilities");
const statClosedFacilities = document.getElementById("statClosedFacilities");
const statMaintenanceFacilities = document.getElementById("statMaintenanceFacilities");
const statAccessibleFacilities = document.getElementById("statAccessibleFacilities");

const facilityForm = document.getElementById("facilityForm");
const facilityIdInput = document.getElementById("facilityId");
const facilityVenueIdInput = document.getElementById("facilityVenueId");
const facilityNameInput = document.getElementById("facilityName");
const facilityTypeInput = document.getElementById("facilityType");
const facilityStatusInput = document.getElementById("facilityStatus");
const facilityZoneInput = document.getElementById("facilityZone");
const facilityFloorInput = document.getElementById("facilityFloor");
const facilityIsAccessibleInput = document.getElementById("facilityIsAccessible");
const facilityDescriptionInput = document.getElementById("facilityDescription");
const facilityDirectionsInput = document.getElementById("facilityDirections");
const facilityFormTitle = document.getElementById("facilityFormTitle");
const facilityFormFeedback = document.getElementById("facilityFormFeedback");
const saveFacilityButton = document.getElementById("saveFacilityButton");
const cancelFacilityEditButton = document.getElementById("cancelFacilityEditButton");

const facilitySearchInput = document.getElementById("facilitySearchInput");
const facilityStatusFilter = document.getElementById("facilityStatusFilter");
const facilityTypeFilter = document.getElementById("facilityTypeFilter");
const clearFacilityFiltersButton = document.getElementById("clearFacilityFiltersButton");

const facilityList = document.getElementById("facilityList");


let venues = [];
let currentVenueId = null;

let facilities = [];

let searchTerm = "";
let statusFilterValue = "";
let typeFilterValue = "";


document.addEventListener("DOMContentLoaded", async () => {
    if (!requireAdministrator()) {
        return;
    }

    renderAdminSidebar("facilities");
    setupAdminMobileMenu();

    adminShell.hidden = false;
    facilityAdminBody.hidden = false;

    await initializePage();
});


async function initializePage() {
    try {
        hideFeedback();

        venues = await api.get("/api/Venues");

        [venueSelect, facilityVenueIdInput].forEach((select) => {
            select.innerHTML = `<option value="">Select a venue</option>`;

            venues.forEach((venue) => {
                const option = document.createElement("option");
                option.value = venue.id;
                option.textContent = venue.name;
                select.appendChild(option);
            });
        });

        const requestedVenueId =
            Number(new URLSearchParams(window.location.search).get("venueId"));

        if (requestedVenueId && venues.some((venue) => venue.id === requestedVenueId)) {
            venueSelect.value = String(requestedVenueId);
            currentVenueId = requestedVenueId;
            resetFacilityForm();
            await loadFacilitiesForVenue();
        }
    } catch (error) {
        showFeedback(
            error.message || "Unable to load venues.",
            "error"
        );
    }
}


venueSelect.addEventListener("change", async () => {
    const value = venueSelect.value;

    if (!value) {
        currentVenueId = null;
        facilities = [];

        facilityManagementPanel.hidden = true;
        noVenueSelectedHint.hidden = false;
        return;
    }

    currentVenueId = Number(value);
    resetFacilityForm();
    await loadFacilitiesForVenue();
});


async function loadFacilitiesForVenue() {
    try {
        hideFeedback();
        noVenueSelectedHint.hidden = true;
        facilityManagementPanel.hidden = true;
        facilitiesLoadingState.hidden = false;

        facilities = await api.get(`/api/venues/${currentVenueId}/facilities`);

        facilitiesLoadingState.hidden = true;
        facilityManagementPanel.hidden = false;

        renderSummary();
        renderFacilityList();
    } catch (error) {
        facilitiesLoadingState.hidden = true;

        if (error?.status === 404) {
            showFeedback("Venue not found.", "error");
            return;
        }

        showFeedback(
            error.message || "Unable to load facilities.",
            "error"
        );
    }
}


/* ---------------- Real, client-derived summary ---------------- */

function renderSummary() {
    statTotalFacilities.textContent = facilities.length;

    statOpenFacilities.textContent = facilities.filter(
        (facility) => facility.status === "Open"
    ).length;

    statClosedFacilities.textContent = facilities.filter(
        (facility) => facility.status === "Closed"
    ).length;

    statMaintenanceFacilities.textContent = facilities.filter(
        (facility) => facility.status === "UnderMaintenance"
    ).length;

    statAccessibleFacilities.textContent = facilities.filter(
        (facility) => facility.isAccessible
    ).length;
}


/* ---------------- Search + Filter (client-side only, no new API calls) ---------------- */

function getFilteredFacilities() {
    const term = searchTerm.trim().toLowerCase();

    return facilities.filter((facility) => {
        const matchesStatus =
            !statusFilterValue || facility.status === statusFilterValue;

        const matchesType =
            !typeFilterValue || facility.facilityType === typeFilterValue;

        const matchesSearch =
            !term ||
            facility.name.toLowerCase().includes(term) ||
            (facility.description || "").toLowerCase().includes(term) ||
            (facility.zone || "").toLowerCase().includes(term) ||
            (facility.floor || "").toLowerCase().includes(term);

        return matchesStatus && matchesType && matchesSearch;
    });
}


facilitySearchInput.addEventListener("input", () => {
    searchTerm = facilitySearchInput.value;
    renderFacilityList();
});


facilityStatusFilter.addEventListener("change", () => {
    statusFilterValue = facilityStatusFilter.value;
    renderFacilityList();
});


facilityTypeFilter.addEventListener("change", () => {
    typeFilterValue = facilityTypeFilter.value;
    renderFacilityList();
});


clearFacilityFiltersButton.addEventListener("click", () => {
    searchTerm = "";
    statusFilterValue = "";
    typeFilterValue = "";
    facilitySearchInput.value = "";
    facilityStatusFilter.value = "";
    facilityTypeFilter.value = "";
    renderFacilityList();
});


/* ---------------- Facility list, grouped by real Type ---------------- */

function renderFacilityList() {
    facilityList.innerHTML = "";

    if (!facilities.length) {
        facilityList.innerHTML = `
            <div class="admin-empty-note">
                <p>No facilities configured yet.</p>
                <button type="button" id="addFirstFacilityButton" class="btn btn-primary btn-small">
                    Add First Facility
                </button>
            </div>
        `;

        document.getElementById("addFirstFacilityButton")
            .addEventListener("click", focusAddFacilityForm);

        return;
    }

    const filtered = getFilteredFacilities();

    if (!filtered.length) {
        facilityList.innerHTML = `
            <p class="admin-empty-note">
                No facilities match your current filters.
            </p>
        `;
        return;
    }

    Object.keys(FACILITY_TYPE).forEach((typeName) => {
        const facilitiesOfType = filtered
            .filter((facility) => facility.facilityType === typeName)
            .sort((a, b) => a.name.localeCompare(b.name));

        if (!facilitiesOfType.length) {
            return;
        }

        const group = document.createElement("div");
        group.className = "facility-type-group";

        const heading = document.createElement("h4");
        heading.className = "facility-type-heading";
        heading.innerHTML = `
            ${escapeHtml(FACILITY_TYPE_LABEL[typeName])}
            <span class="facility-type-count">(${facilitiesOfType.length})</span>
        `;

        group.appendChild(heading);

        facilitiesOfType.forEach((facility) => {
            group.appendChild(buildFacilityRow(facility));
        });

        facilityList.appendChild(group);
    });
}


function buildFacilityRow(facility) {
    const row = document.createElement("div");
    row.className = "facility-row";

    const metaParts = [facility.zone, facility.floor]
        .filter((part) => Boolean(part))
        .map((part) => escapeHtml(part));

    row.innerHTML = `
        <div class="facility-main">
            <span class="facility-name">${escapeHtml(facility.name)}</span>
            ${metaParts.length ? `<span class="facility-meta">${metaParts.join(" &middot; ")}</span>` : ""}
        </div>

        <div class="facility-badges">
            <span class="facility-status-badge facility-status-${facility.status.toLowerCase()}">
                ${escapeHtml(FACILITY_STATUS_LABEL[facility.status] || facility.status)}
            </span>
            <span class="facility-accessibility-badge${facility.isAccessible ? "" : " is-not-accessible"}">
                ${facility.isAccessible ? "Accessible" : "Not Accessible"}
            </span>
        </div>

        <div class="facility-actions">
            <button type="button" class="btn btn-primary btn-small edit-facility-button" data-id="${facility.id}">
                Edit
            </button>
            <button type="button" class="btn btn-secondary btn-small delete-facility-button" data-id="${facility.id}">
                Delete
            </button>
        </div>
    `;

    row.querySelector(".edit-facility-button")
        .addEventListener("click", () => startEditFacility(facility.id));

    row.querySelector(".delete-facility-button")
        .addEventListener("click", () => deleteFacility(facility.id));

    return row;
}


/* ---------------- Add / Edit form ---------------- */

function resetFacilityForm() {
    facilityForm.reset();
    facilityIdInput.value = "";

    facilityVenueIdInput.value = currentVenueId ? String(currentVenueId) : "";
    facilityTypeInput.value = String(FACILITY_TYPE.Washroom);
    facilityStatusInput.value = String(FACILITY_STATUS.Open);
    facilityIsAccessibleInput.value = "false";

    facilityFormTitle.textContent = "Add Facility";
    saveFacilityButton.textContent = "Save Facility";
    cancelFacilityEditButton.hidden = true;

    hideFacilityFormFeedback();
}


function startEditFacility(facilityId) {
    const facility = facilities.find((item) => item.id === facilityId);

    if (!facility) {
        return;
    }

    facilityIdInput.value = facility.id;
    facilityVenueIdInput.value = facility.venueId;
    facilityNameInput.value = facility.name;
    facilityTypeInput.value = String(FACILITY_TYPE[facility.facilityType] || FACILITY_TYPE.Washroom);
    facilityStatusInput.value = String(FACILITY_STATUS[facility.status] || FACILITY_STATUS.Open);
    facilityZoneInput.value = facility.zone || "";
    facilityFloorInput.value = facility.floor || "";
    facilityIsAccessibleInput.value = String(facility.isAccessible);
    facilityDescriptionInput.value = facility.description || "";
    facilityDirectionsInput.value = facility.directions || "";

    facilityFormTitle.textContent = "Edit Facility";
    saveFacilityButton.textContent = "Update Facility";
    cancelFacilityEditButton.hidden = false;

    hideFacilityFormFeedback();

    facilityForm.scrollIntoView({ behavior: "smooth", block: "start" });
}


function focusAddFacilityForm() {
    resetFacilityForm();
    facilityForm.scrollIntoView({ behavior: "smooth", block: "start" });
    facilityNameInput.focus({ preventScroll: true });
}


async function saveFacility(event) {
    event.preventDefault();

    const facilityId = facilityIdInput.value;
    const venueId = Number(facilityVenueIdInput.value);

    const payload = {
        venueId,
        name: facilityNameInput.value.trim(),
        facilityType: Number(facilityTypeInput.value),
        zone: facilityZoneInput.value.trim() || null,
        floor: facilityFloorInput.value.trim() || null,
        description: facilityDescriptionInput.value.trim() || null,
        isAccessible: facilityIsAccessibleInput.value === "true",
        status: Number(facilityStatusInput.value),
        directions: facilityDirectionsInput.value.trim() || null
    };

    if (!venueId) {
        showFacilityFormFeedback("Select a venue.", "error");
        return;
    }

    if (!payload.name) {
        showFacilityFormFeedback("Facility name is required.", "error");
        return;
    }

    try {
        setButtonLoading(saveFacilityButton, true, facilityId ? "Updating..." : "Saving...");
        hideFacilityFormFeedback();

        if (facilityId) {
            await api.put(`/api/Facilities/${facilityId}`, payload);
            showFeedback("Facility updated successfully.", "success");
        } else {
            await api.post(`/api/venues/${venueId}/facilities`, payload);
            showFeedback("Facility created successfully.", "success");
        }

        resetFacilityForm();

        if (currentVenueId) {
            await loadFacilitiesForVenue();
        }
    } catch (error) {
        showFacilityFormFeedback(
            error?.data?.message || error.message || "Unable to save facility.",
            "error"
        );
    } finally {
        setButtonLoading(saveFacilityButton, false);
    }
}


async function deleteFacility(facilityId) {
    const facility = facilities.find((item) => item.id === facilityId);

    if (!facility) {
        return;
    }

    const confirmed = window.confirm(`Delete facility "${facility.name}"?`);

    if (!confirmed) {
        return;
    }

    try {
        hideFeedback();

        await api.delete(`/api/Facilities/${facilityId}`);

        showFeedback("Facility deleted successfully.", "success");
        await loadFacilitiesForVenue();
    } catch (error) {
        showFeedback(
            error?.data?.message || error.message || "Unable to delete facility.",
            "error"
        );
    }
}


function showFacilityFormFeedback(message, type) {
    facilityFormFeedback.textContent = message;
    facilityFormFeedback.className = `feedback feedback-${type}`;
    facilityFormFeedback.hidden = false;
}


function hideFacilityFormFeedback() {
    facilityFormFeedback.hidden = true;
    facilityFormFeedback.textContent = "";
}


function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}


facilityForm.addEventListener("submit", saveFacility);

cancelFacilityEditButton.addEventListener("click", () => {
    resetFacilityForm();
    hideFeedback();
});
