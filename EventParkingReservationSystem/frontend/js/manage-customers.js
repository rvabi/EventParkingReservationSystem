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
 * CustomerDto (CustomersController.MapToDto) only ever returns Id,
 * FullName, Email, Phone, Role, Status, EmailVerified, CreatedAt,
 * UpdatedAt - no password/token/security data exists on the DTO to
 * accidentally expose. CustomerStatus mirrors
 * EventParking.Models.Enums.CustomerStatus exactly (Active = 1,
 * Deactivated = 2 - there is no separate "Inactive" value). GET
 * /api/Customers (GetAll) applies no role filter, so Administrator
 * accounts appear in this list too - the Role tag on each row makes that
 * visible rather than hiding it.
 *
 * The only Administrator mutations that exist are
 * PATCH /api/Customers/{id}/deactivate and
 * PATCH /api/Customers/{id}/reactivate (CustomerService.DeactivateAsync /
 * ReactivateAsync). There is no Delete, no role-change, and no
 * Admin-triggered email verification endpoint - none of those actions are
 * rendered here.
 */


const adminShell = document.getElementById("adminShell");
const customerAdminBody = document.getElementById("customerAdminBody");
const customersLoadingState = document.getElementById("customersLoadingState");

const statTotalCustomers = document.getElementById("statTotalCustomers");
const statVerifiedCustomers = document.getElementById("statVerifiedCustomers");
const statUnverifiedCustomers = document.getElementById("statUnverifiedCustomers");
const statActiveCustomers = document.getElementById("statActiveCustomers");
const statDeactivatedCustomers = document.getElementById("statDeactivatedCustomers");

const customerFilterNote = document.getElementById("customerFilterNote");

const customerSearchInput = document.getElementById("customerSearchInput");
const customerVerificationFilter = document.getElementById("customerVerificationFilter");
const customerStatusFilter = document.getElementById("customerStatusFilter");
const customerSortSelect = document.getElementById("customerSortSelect");
const clearCustomerFiltersButton = document.getElementById("clearCustomerFiltersButton");

const customerActionFeedback = document.getElementById("customerActionFeedback");
const customerList = document.getElementById("customerList");


let customers = [];

let searchTerm = "";
let verificationFilterValue = "";
let statusFilterValue = "";
let sortValue = "newest";


document.addEventListener("DOMContentLoaded", async () => {
    if (!requireAdministrator()) {
        return;
    }

    renderAdminSidebar("customers");
    setupAdminMobileMenu();

    adminShell.hidden = false;

    await initializePage();
});


async function initializePage() {
    try {
        hideFeedback();
        customersLoadingState.hidden = false;
        customerAdminBody.hidden = true;

        customers = await api.get("/api/Customers");

        customersLoadingState.hidden = true;
        customerAdminBody.hidden = false;

        renderSummary();
        renderCustomerList();
    } catch (error) {
        customersLoadingState.hidden = true;

        showFeedback(
            error?.data?.message || error.message || "Unable to load customers.",
            "error"
        );
    }
}


/* ---------------- Real, client-derived summary (unfiltered totals) ---------------- */

function renderSummary() {
    statTotalCustomers.textContent = customers.length;

    statVerifiedCustomers.textContent = customers.filter(
        (customer) => customer.emailVerified
    ).length;

    statUnverifiedCustomers.textContent = customers.filter(
        (customer) => !customer.emailVerified
    ).length;

    statActiveCustomers.textContent = customers.filter(
        (customer) => customer.status === "Active"
    ).length;

    statDeactivatedCustomers.textContent = customers.filter(
        (customer) => customer.status === "Deactivated"
    ).length;
}


/* ---------------- Search + Filter + Sort (client-side only) ---------------- */

function getFilteredSortedCustomers() {
    const term = searchTerm.trim().toLowerCase();

    const filtered = customers.filter((customer) => {
        const matchesVerification =
            !verificationFilterValue ||
            (verificationFilterValue === "verified" && customer.emailVerified) ||
            (verificationFilterValue === "unverified" && !customer.emailVerified);

        const matchesStatus =
            !statusFilterValue || customer.status === statusFilterValue;

        const matchesSearch =
            !term ||
            customer.fullName.toLowerCase().includes(term) ||
            customer.email.toLowerCase().includes(term) ||
            (customer.phone || "").toLowerCase().includes(term) ||
            String(customer.id).includes(term);

        return matchesVerification && matchesStatus && matchesSearch;
    });

    const sorted = filtered.slice();

    if (sortValue === "oldest") {
        sorted.sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt));
    } else if (sortValue === "nameAsc") {
        sorted.sort((a, b) => a.fullName.localeCompare(b.fullName));
    } else if (sortValue === "nameDesc") {
        sorted.sort((a, b) => b.fullName.localeCompare(a.fullName));
    } else {
        sorted.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
    }

    return sorted;
}


customerSearchInput.addEventListener("input", () => {
    searchTerm = customerSearchInput.value;
    renderCustomerList();
});


customerVerificationFilter.addEventListener("change", () => {
    verificationFilterValue = customerVerificationFilter.value;
    renderCustomerList();
});


customerStatusFilter.addEventListener("change", () => {
    statusFilterValue = customerStatusFilter.value;
    renderCustomerList();
});


customerSortSelect.addEventListener("change", () => {
    sortValue = customerSortSelect.value;
    renderCustomerList();
});


clearCustomerFiltersButton.addEventListener("click", () => {
    searchTerm = "";
    verificationFilterValue = "";
    statusFilterValue = "";
    sortValue = "newest";

    customerSearchInput.value = "";
    customerVerificationFilter.value = "";
    customerStatusFilter.value = "";
    customerSortSelect.value = "newest";

    renderCustomerList();
});


/* ---------------- Customer list ---------------- */

function renderCustomerList() {
    customerList.innerHTML = "";

    if (!customers.length) {
        customerList.innerHTML = `
            <p class="admin-empty-note">
                No customers found.
            </p>
        `;
        return;
    }

    const filtered = getFilteredSortedCustomers();

    if (!filtered.length) {
        customerList.innerHTML = `
            <p class="admin-empty-note">
                No customers match your current filters.
            </p>
        `;
        return;
    }

    filtered.forEach((customer) => {
        customerList.appendChild(buildCustomerRow(customer));
    });
}


function buildCustomerRow(customer) {
    const row = document.createElement("div");
    row.className = "customer-row";

    const isActive = customer.status === "Active";

    row.innerHTML = `
        <div class="customer-header">
            <span class="customer-name">${escapeHtml(customer.fullName)}</span>
            <span class="customer-role-tag">${escapeHtml(customer.role)}</span>
            <span class="customer-time">Joined ${formatDateTime(customer.createdAt)}</span>
        </div>

        <div class="customer-meta">
            <span>${escapeHtml(customer.email)}</span>
            ${customer.phone ? `<span>${escapeHtml(customer.phone)}</span>` : ""}
            <span>Customer #${customer.id}</span>
        </div>

        <div class="customer-footer">
            <span class="customer-verified-badge${customer.emailVerified ? "" : " is-unverified"}">
                ${customer.emailVerified ? "Verified" : "Unverified"}
            </span>
            <span class="customer-status-badge customer-status-${customer.status.toLowerCase()}">
                ${escapeHtml(customer.status)}
            </span>

            <div class="customer-actions">
                <button type="button" class="btn btn-secondary btn-small view-details-button">
                    View Details
                </button>
                ${isActive
                    ? `<button type="button" class="btn btn-secondary btn-small deactivate-button">Deactivate</button>`
                    : `<button type="button" class="btn btn-primary btn-small reactivate-button">Reactivate</button>`
                }
            </div>
        </div>

        <div class="customer-detail-panel" hidden>
            ${buildCustomerDetailHtml(customer)}
        </div>
    `;

    const detailPanel = row.querySelector(".customer-detail-panel");
    const viewDetailsButton = row.querySelector(".view-details-button");

    viewDetailsButton.addEventListener("click", () => {
        const isHidden = detailPanel.hidden;
        detailPanel.hidden = !isHidden;
        viewDetailsButton.textContent = isHidden ? "Hide Details" : "View Details";
    });

    const deactivateButton = row.querySelector(".deactivate-button");
    const reactivateButton = row.querySelector(".reactivate-button");

    if (deactivateButton) {
        deactivateButton.addEventListener("click", () => deactivateCustomer(customer));
    }

    if (reactivateButton) {
        reactivateButton.addEventListener("click", () => reactivateCustomer(customer));
    }

    return row;
}


function buildCustomerDetailHtml(customer) {
    return `
        <div class="customer-detail-grid">

            <div>
                <span class="customer-detail-label">Customer ID</span>
                <span class="customer-detail-value">#${customer.id}</span>
            </div>

            <div>
                <span class="customer-detail-label">Role</span>
                <span class="customer-detail-value">${escapeHtml(customer.role)}</span>
            </div>

            <div>
                <span class="customer-detail-label">Email</span>
                <span class="customer-detail-value">${escapeHtml(customer.email)}</span>
            </div>

            <div>
                <span class="customer-detail-label">Phone</span>
                <span class="customer-detail-value">${escapeHtml(customer.phone || "-")}</span>
            </div>

            <div>
                <span class="customer-detail-label">Created At</span>
                <span class="customer-detail-value">${formatDateTime(customer.createdAt)}</span>
            </div>

            ${customer.updatedAt ? `
                <div>
                    <span class="customer-detail-label">Updated At</span>
                    <span class="customer-detail-value">${formatDateTime(customer.updatedAt)}</span>
                </div>
            ` : ""}

        </div>

        <a href="./manage-bookings.html?customerId=${customer.id}" class="btn btn-secondary btn-small">
            View Bookings
        </a>
    `;
}


/* ---------------- Activate / Deactivate (only two real mutations) ---------------- */

async function deactivateCustomer(customer) {
    const roleNote = customer.role === "Administrator" ? " (this account has the Administrator role)" : "";

    const confirmed = window.confirm(
        `Deactivate ${customer.fullName}'s account${roleNote}?`
    );

    if (!confirmed) {
        return;
    }

    try {
        hideCustomerActionFeedback();

        const response = await api.patch(`/api/Customers/${customer.id}/deactivate`);

        showCustomerActionFeedback(
            response?.message || "Customer account deactivated successfully.",
            "success"
        );

        await reloadCustomers();
    } catch (error) {
        showCustomerActionFeedback(
            error?.data?.message || error.message || "Unable to deactivate this customer.",
            "error"
        );
    }
}


async function reactivateCustomer(customer) {
    const confirmed = window.confirm(
        `Reactivate ${customer.fullName}'s account?`
    );

    if (!confirmed) {
        return;
    }

    try {
        hideCustomerActionFeedback();

        const response = await api.patch(`/api/Customers/${customer.id}/reactivate`);

        showCustomerActionFeedback(
            response?.message || "Customer account reactivated successfully.",
            "success"
        );

        await reloadCustomers();
    } catch (error) {
        showCustomerActionFeedback(
            error?.data?.message || error.message || "Unable to reactivate this customer.",
            "error"
        );
    }
}


async function reloadCustomers() {
    customers = await api.get("/api/Customers");
    renderSummary();
    renderCustomerList();
}


function showCustomerActionFeedback(message, type) {
    customerActionFeedback.textContent = message;
    customerActionFeedback.className = `feedback feedback-${type}`;
    customerActionFeedback.hidden = false;
}


function hideCustomerActionFeedback() {
    customerActionFeedback.hidden = true;
    customerActionFeedback.textContent = "";
}


function formatDateTime(value) {
    if (!value) {
        return "-";
    }

    return new Date(value).toLocaleString();
}


function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
