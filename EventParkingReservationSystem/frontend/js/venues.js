import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";


const venueForm =
    document.getElementById("venueForm");

const venueIdInput =
    document.getElementById("venueId");

const venueNameInput =
    document.getElementById("venueName");

const venueAddressInput =
    document.getElementById("venueAddress");

const venueCapacityInput =
    document.getElementById("venueCapacity");

const venueList =
    document.getElementById("venueList");

const venueFormTitle =
    document.getElementById("venueFormTitle");

const saveVenueButton =
    document.getElementById("saveVenueButton");

const cancelEditButton =
    document.getElementById("cancelEditButton");


let venues = [];


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

        renderVenues();
    } catch (error) {
        venueList.innerHTML = "";

        showFeedback(
            error.message ||
            "Unable to load venues.",
            "error"
        );
    }
}


function renderVenues() {
    venueList.innerHTML = "";

    if (!venues || venues.length === 0) {
        showFeedback(
            "No venues are available.",
            "info"
        );

        return;
    }

    hideFeedback();

    venues.forEach((venue) => {

        const card =
            document.createElement("article");

        card.className =
            "service-card";

        card.innerHTML = `
            <div class="service-number">
                #${venue.id}
            </div>

            <div class="service-icon">
                V
            </div>

            <h3>
                ${escapeHtml(venue.name)}
            </h3>

            <p>
                <strong>Address:</strong>
                ${escapeHtml(venue.address)}
            </p>

            <p>
                <strong>Total Capacity:</strong>
                ${venue.totalCapacity}
            </p>

            <div
                class="hero-actions"
                style="margin-top: auto;">

                <button
                    class="btn btn-primary edit-venue-button"
                    type="button"
                    data-id="${venue.id}">
                    Edit
                </button>

                <button
                    class="btn btn-secondary delete-venue-button"
                    type="button"
                    data-id="${venue.id}">
                    Delete
                </button>

            </div>
        `;

        venueList.appendChild(card);
    });


    attachVenueActionEvents();
}


function attachVenueActionEvents() {

    document
        .querySelectorAll(
            ".edit-venue-button"
        )
        .forEach((button) => {

            button.addEventListener(
                "click",
                () => {
                    startEditVenue(
                        Number(button.dataset.id)
                    );
                }
            );
        });


    document
        .querySelectorAll(
            ".delete-venue-button"
        )
        .forEach((button) => {

            button.addEventListener(
                "click",
                async () => {
                    await deleteVenue(
                        Number(button.dataset.id)
                    );
                }
            );
        });
}


function startEditVenue(venueId) {

    const venue =
        venues.find(
            (item) =>
                item.id === venueId
        );

    if (!venue) {
        return;
    }

    venueIdInput.value =
        venue.id;

    venueNameInput.value =
        venue.name;

    venueAddressInput.value =
        venue.address;

    venueCapacityInput.value =
        venue.totalCapacity;

    venueFormTitle.textContent =
        "Edit Venue";

    saveVenueButton.textContent =
        "Update Venue";

    cancelEditButton.hidden =
        false;

    venueForm.scrollIntoView({
        behavior: "smooth",
        block: "start"
    });
}


function resetVenueForm() {

    venueForm.reset();

    venueIdInput.value = "";

    venueFormTitle.textContent =
        "Add New Venue";

    saveVenueButton.textContent =
        "Save Venue";

    cancelEditButton.hidden =
        true;
}


async function saveVenue(event) {

    event.preventDefault();

    const venueId =
        venueIdInput.value;

    const venueData = {
        name:
            venueNameInput.value.trim(),

        address:
            venueAddressInput.value.trim(),

        totalCapacity:
            Number(
                venueCapacityInput.value
            )
    };


    if (
        !venueData.name ||
        !venueData.address ||
        venueData.totalCapacity <= 0
    ) {
        showFeedback(
            "Please enter valid venue details.",
            "error"
        );

        return;
    }


    try {
        setButtonLoading(
            saveVenueButton,
            true,
            venueId
                ? "Updating..."
                : "Saving..."
        );

        hideFeedback();


        if (venueId) {

            await api.put(
                `/api/Venues/${venueId}`,
                venueData
            );

            showFeedback(
                "Venue updated successfully.",
                "success"
            );

        } else {

            await api.post(
                "/api/Venues",
                venueData
            );

            showFeedback(
                "Venue created successfully.",
                "success"
            );
        }


        resetVenueForm();

        await loadVenues();

    } catch (error) {

        showFeedback(
            error.message ||
            "Unable to save venue.",
            "error"
        );

    } finally {

        setButtonLoading(
            saveVenueButton,
            false
        );
    }
}


async function deleteVenue(venueId) {

    const venue =
        venues.find(
            (item) =>
                item.id === venueId
        );

    if (!venue) {
        return;
    }


    const confirmed =
        window.confirm(
            `Delete "${venue.name}"?`
        );

    if (!confirmed) {
        return;
    }


    try {
        hideFeedback();

        await api.delete(
            `/api/Venues/${venueId}`
        );

        showFeedback(
            "Venue deleted successfully.",
            "success"
        );

        await loadVenues();

    } catch (error) {

        showFeedback(
            error.message ||
            "Unable to delete venue. The venue may have upcoming events.",
            "error"
        );
    }
}


function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}


venueForm.addEventListener(
    "submit",
    saveVenue
);


cancelEditButton.addEventListener(
    "click",
    () => {
        resetVenueForm();
        hideFeedback();
    }
);