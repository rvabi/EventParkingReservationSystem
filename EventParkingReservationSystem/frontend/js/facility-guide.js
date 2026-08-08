import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback
} from "./ui.js";


const venueIdInput =
    document.getElementById("venueId");

const facilityList =
    document.getElementById("facilityList");


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


        facilityList.innerHTML = "";

        showFeedback(
            "Select a venue to view its facilities.",
            "info"
        );

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


    if (!venueId) {

        showFeedback(
            "Select a venue to view its facilities.",
            "info"
        );

        return;
    }


    try {

        hideFeedback();

        const facilities =
            await api.get(
                `/api/venues/${venueId}/facilities`
            );


        renderFacilities(facilities);

    } catch (error) {

        showFeedback(
            error.message ||
            "Unable to load facility guidance.",
            "error"
        );
    }
}


function renderFacilities(facilities) {

    facilityList.innerHTML = "";


    if (!facilities || facilities.length === 0) {

        showFeedback(
            "No facility information is available for this venue.",
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
                ${getFacilityIcon(
                    facility.facilityType
                )}
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
                ${escapeHtml(
                    facility.zone || "-"
                )}
            </p>

            <p>
                <strong>Floor:</strong>
                ${escapeHtml(
                    facility.floor || "-"
                )}
            </p>

            <p>
                <strong>Status:</strong>
                ${getFacilityStatusName(
                    facility.status
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
                <strong>Description:</strong>
                ${escapeHtml(
                    facility.description ||
                    "No description available."
                )}
            </p>

            <p>
                <strong>Directions:</strong>
                ${escapeHtml(
                    facility.directions ||
                    "No directions available."
                )}
            </p>
        `;


        facilityList.appendChild(card);
    });
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
        "Facility";
}


function getFacilityStatusName(status) {

    const statuses = {
        1: "Open",
        2: "Closed",
        3: "Under Maintenance"
    };

    return statuses[status] ||
        "Unknown";
}


function getFacilityIcon(type) {

    const icons = {
        1: "W",
        2: "+",
        3: "P",
        4: "$",
        5: "i",
        6: "→"
    };

    return icons[type] || "F";
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

        await loadFacilities(
            Number(venueIdInput.value)
        );
    }
);