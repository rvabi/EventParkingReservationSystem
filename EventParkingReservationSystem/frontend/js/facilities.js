import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";


const facilityForm =
    document.getElementById("facilityForm");

const facilityIdInput =
    document.getElementById("facilityId");

const venueIdInput =
    document.getElementById("venueId");

const facilityNameInput =
    document.getElementById("facilityName");

const facilityTypeInput =
    document.getElementById("facilityType");

const facilityZoneInput =
    document.getElementById("facilityZone");

const facilityFloorInput =
    document.getElementById("facilityFloor");

const facilityDescriptionInput =
    document.getElementById("facilityDescription");

const facilityStatusInput =
    document.getElementById("facilityStatus");

const isAccessibleInput =
    document.getElementById("isAccessible");

const facilityDirectionsInput =
    document.getElementById("facilityDirections");

const facilityList =
    document.getElementById("facilityList");

const facilityFormTitle =
    document.getElementById("facilityFormTitle");

const saveFacilityButton =
    document.getElementById("saveFacilityButton");

const cancelEditButton =
    document.getElementById("cancelEditButton");


let venues = [];
let facilities = [];


document.addEventListener(
    "DOMContentLoaded",
    async () => {

        renderNavbar();

        await loadVenues();
    }
);


async function loadVenues() {

    try {

        hideFeedback();

        venues =
            await api.get("/api/Venues");

        venueIdInput.innerHTML = `
            <option value="">
                Select Venue
            </option>
        `;


        venues.forEach((venue) => {

            const option =
                document.createElement("option");

            option.value =
                venue.id;

            option.textContent =
                venue.name;

            venueIdInput.appendChild(option);
        });

    } catch (error) {

        showFeedback(
            error.message ||
            "Unable to load venues.",
            "error"
        );
    }
}


async function loadFacilities(venueId) {

    facilityList.innerHTML = "";

    facilities = [];


    if (!venueId) {

        showFeedback(
            "Select a venue to view its facilities.",
            "info"
        );

        return;
    }


    try {

        hideFeedback();

        facilities =
            await api.get(
                `/api/venues/${venueId}/facilities`
            );

        renderFacilities();

    } catch (error) {

        showFeedback(
            error.message ||
            "Unable to load facilities.",
            "error"
        );
    }
}


function renderFacilities() {

    facilityList.innerHTML = "";


    if (!facilities || facilities.length === 0) {

        showFeedback(
            "No facilities are available for this venue.",
            "info"
        );

        return;
    }


    hideFeedback();


    facilities.forEach((facility) => {

        const card =
            document.createElement("article");

        card.className =
            "service-card";


        card.innerHTML = `

            <div class="service-number">
                #${facility.id}
            </div>

            <div class="service-icon">
                F
            </div>

            <h3>
                ${escapeHtml(facility.name)}
            </h3>

            <p>
                <strong>Type:</strong>
                ${getFacilityTypeName(
                    facility.facilityType
                )}
            </p>

            <p>
                <strong>Zone:</strong>
                ${escapeHtml(facility.zone)}
            </p>

            <p>
                <strong>Floor:</strong>
                ${escapeHtml(facility.floor)}
            </p>

            <p>
                <strong>Description:</strong>
                ${escapeHtml(
                    facility.description
                )}
            </p>

            <p>
                <strong>Status:</strong>
                ${getFacilityStatusName(
                    facility.facilityStatus
                )}
            </p>

            <p>
                <strong>Accessibility:</strong>
                ${
                    facility.isAccessible
                        ? "Accessible"
                        : "Not Accessible"
                }
            </p>

            <p>
                <strong>Directions:</strong>
                ${escapeHtml(
                    facility.directions
                )}
            </p>


            <div
                class="hero-actions"
                style="margin-top: auto;">

                <button
                    class="btn btn-primary edit-facility-button"
                    type="button"
                    data-id="${facility.id}">
                    Edit
                </button>

                <button
                    class="btn btn-secondary delete-facility-button"
                    type="button"
                    data-id="${facility.id}">
                    Delete
                </button>

            </div>
        `;


        facilityList.appendChild(card);
    });


    attachFacilityActionEvents();
}


function attachFacilityActionEvents() {

    document
        .querySelectorAll(
            ".edit-facility-button"
        )
        .forEach((button) => {

            button.addEventListener(
                "click",
                () => {

                    startEditFacility(
                        Number(button.dataset.id)
                    );
                }
            );
        });


    document
        .querySelectorAll(
            ".delete-facility-button"
        )
        .forEach((button) => {

            button.addEventListener(
                "click",
                async () => {

                    await deleteFacility(
                        Number(button.dataset.id)
                    );
                }
            );
        });
}


function startEditFacility(facilityId) {

    const facility =
        facilities.find(
            (item) =>
                item.id === facilityId
        );


    if (!facility) {
        return;
    }


    facilityIdInput.value =
        facility.id;

    venueIdInput.value =
        facility.venueId;

    facilityNameInput.value =
        facility.name;

    facilityTypeInput.value =
        facility.facilityType;

    facilityZoneInput.value =
        facility.zone;

    facilityFloorInput.value =
        facility.floor;

    facilityDescriptionInput.value =
        facility.description || "";

    facilityStatusInput.value =
        facility.facilityStatus;

    isAccessibleInput.value =
        String(facility.isAccessible);

    facilityDirectionsInput.value =
        facility.directions || "";


    venueIdInput.disabled =
        true;

    facilityFormTitle.textContent =
        "Edit Facility";

    saveFacilityButton.textContent =
        "Update Facility";

    cancelEditButton.hidden =
        false;


    facilityForm.scrollIntoView({
        behavior: "smooth",
        block: "start"
    });
}


function resetFacilityForm() {

    const selectedVenueId =
        venueIdInput.value;


    facilityForm.reset();

    facilityIdInput.value = "";

    venueIdInput.disabled =
        false;

    venueIdInput.value =
        selectedVenueId;

    facilityFormTitle.textContent =
        "Add New Facility";

    saveFacilityButton.textContent =
        "Save Facility";

    cancelEditButton.hidden =
        true;
}


async function saveFacility(event) {

    event.preventDefault();


    const facilityId =
        facilityIdInput.value;

    const venueId =
        Number(venueIdInput.value);


    const facilityData = {

        venueId: venueId,

        name:
            facilityNameInput.value.trim(),

        facilityType:
            Number(facilityTypeInput.value),

        zone:
            facilityZoneInput.value.trim(),

        floor:
            facilityFloorInput.value.trim(),

        description:
            facilityDescriptionInput.value.trim(),

        isAccessible:
            isAccessibleInput.value === "true",

        Status:
            Number(facilityStatusInput.value),

        directions:
            facilityDirectionsInput.value.trim()
    };


    if (
        venueId <= 0 ||
        !facilityData.name ||
        facilityData.facilityType <= 0 ||
        !facilityData.zone ||
        !facilityData.floor ||
        !facilityData.description ||
        facilityData.facilityStatus <= 0 ||
        isAccessibleInput.value === "" ||
        !facilityData.directions
    ) {

        showFeedback(
            "Please enter valid facility details.",
            "error"
        );

        return;
    }


    try {

        setButtonLoading(
            saveFacilityButton,
            true,
            facilityId
                ? "Updating..."
                : "Saving..."
        );

        hideFeedback();


        if (facilityId) {

            await api.put(
                `/api/Facilities/${facilityId}`,
                facilityData
            );

            showFeedback(
                "Facility updated successfully.",
                "success"
            );

        } else {

            await api.post(
                `/api/venues/${venueId}/facilities`,
                facilityData
            );

            showFeedback(
                "Facility created successfully.",
                "success"
            );
        }


        resetFacilityForm();

        await loadFacilities(venueId);

    } catch (error) {

        showFeedback(
            error.message ||
            "Unable to save facility.",
            "error"
        );

    } finally {

        setButtonLoading(
            saveFacilityButton,
            false
        );
    }
}


async function deleteFacility(facilityId) {

    const facility =
        facilities.find(
            (item) =>
                item.id === facilityId
        );


    if (!facility) {
        return;
    }


    const confirmed =
        window.confirm(
            `Delete "${facility.name}"?`
        );


    if (!confirmed) {
        return;
    }


    const venueId =
        facility.venueId;


    try {

        hideFeedback();

        await api.delete(
            `/api/Facilities/${facilityId}`
        );


        showFeedback(
            "Facility deleted successfully.",
            "success"
        );


        await loadFacilities(venueId);

    } catch (error) {

        showFeedback(
            error.message ||
            "Unable to delete facility.",
            "error"
        );
    }
}


function getFacilityTypeName(type) {

    const types = {
        1: "Washroom",
        2: "First Aid",
        3: "Prayer Room",
        4: "ATM",
        5: "Information Desk",
        6: "Exit"
    };

    return types[type] ||
        `Type ${type}`;
}


function getFacilityStatusName(status) {

    const statuses = {
        1: "Open",
        2: "Closed",
        3: "Under Maintenance"
    };

    return statuses[status] ||
        `Status ${status}`;
}


function escapeHtml(value) {

    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}


venueIdInput.addEventListener(
    "change",
    async () => {

        if (facilityIdInput.value) {
            return;
        }

        await loadFacilities(
            Number(venueIdInput.value)
        );
    }
);


facilityForm.addEventListener(
    "submit",
    saveFacility
);


cancelEditButton.addEventListener(
    "click",
    async () => {

        const venueId =
            Number(venueIdInput.value);

        resetFacilityForm();

        hideFeedback();

        if (venueId > 0) {
            await loadFacilities(venueId);
        }
    }
);