import { isAuthenticated, getCustomerRole, removeToken } from "./auth.js";

/*
 * Shared Admin shell: sidebar navigation, mobile drawer toggle and the
 * Administrator role gate, used by every Admin page (admin-dashboard.html,
 * manage-events.html, manage-seats.html) so this logic - and the visual
 * "Admin Control Center" identity - only lives in one place. This is a
 * separate helper from ui.js's renderNavbar() on purpose: Admin pages use a
 * sidebar shell instead of the public/customer top navbar, and reuse
 * auth.js's existing token/role utilities rather than any new auth system.
 *
 * Pages whose management UI does not exist yet are listed with
 * implemented: false so they render as disabled "Coming next" entries
 * instead of dead links.
 */
const ADMIN_NAV_GROUPS = [
    {
        label: "Main",
        items: [
            { id: "dashboard", label: "Dashboard", href: "admin-dashboard.html", implemented: true },
            { id: "events", label: "Events", href: "manage-events.html", implemented: true },
            { id: "seats", label: "Seats", href: "manage-seats.html", implemented: true }
        ]
    },
    {
        label: "Operations",
        items: [
            { id: "parking", label: "Parking", href: "manage-parking.html", implemented: true },
            { id: "food", label: "Food Court", href: null, implemented: false },
            { id: "facilities", label: "Facilities", href: null, implemented: false },
            { id: "bookings", label: "Bookings", href: null, implemented: false },
            { id: "customers", label: "Customers", href: null, implemented: false }
        ]
    }
];


/*
 * Verifies the current visitor is an authenticated Administrator. On
 * failure it shows the page's #authGuardPanel (the same pattern already
 * used on Customer-guarded pages like seat-selection.html) and returns
 * false so the caller can keep the rest of the page hidden - role
 * protection does not rely on the sidebar links alone.
 */
export function requireAdministrator() {
    const authGuardPanel = document.getElementById("authGuardPanel");
    const authGuardMessage = document.getElementById("authGuardMessage");
    const authGuardLink = document.getElementById("authGuardLink");

    function showGuard(message) {
        if (authGuardMessage) {
            authGuardMessage.textContent = message;
        }

        if (authGuardLink) {
            authGuardLink.href = "login.html";
        }

        if (authGuardPanel) {
            authGuardPanel.hidden = false;
        }
    }

    if (!isAuthenticated()) {
        showGuard("Please log in with an administrator account to continue.");
        return false;
    }

    if (getCustomerRole() !== "Administrator") {
        showGuard("This area is only available to administrator accounts.");
        return false;
    }

    return true;
}


/*
 * Fills the #adminSidebarSlot element (expected in every Admin page's
 * static markup) with the sidebar nav + mobile backdrop, and wires the
 * logout button. activePage must match one of the ids above
 * ("dashboard" | "events" | "seats").
 */
export function renderAdminSidebar(activePage) {
    const slot = document.getElementById("adminSidebarSlot");

    if (!slot) {
        return;
    }

    const groupsHtml = ADMIN_NAV_GROUPS.map((group) => `
        <span class="admin-sidebar-group-label">${escapeHtml(group.label)}</span>
        ${group.items.map((item) => renderNavItem(item, activePage)).join("")}
    `).join("");

    slot.innerHTML = `
        <aside class="admin-sidebar">

            <div class="admin-sidebar-brand">
                <span class="admin-sidebar-brand-name">SmartEvent</span>
                <span class="admin-sidebar-brand-tag">Admin Control</span>
            </div>

            <nav class="admin-sidebar-nav" aria-label="Admin navigation">
                ${groupsHtml}
            </nav>

            <div class="admin-sidebar-footer">
                <button id="adminLogoutButton" class="btn btn-secondary btn-small" type="button">
                    Logout
                </button>
            </div>

        </aside>

        <div id="adminSidebarBackdrop" class="admin-sidebar-backdrop"></div>
    `;

    const logoutButton = document.getElementById("adminLogoutButton");

    if (logoutButton) {
        logoutButton.addEventListener("click", () => {
            removeToken();
            window.location.href = "login.html";
        });
    }
}


function renderNavItem(item, activePage) {
    if (!item.implemented) {
        return `
            <span class="admin-sidebar-link is-disabled">
                ${escapeHtml(item.label)}
                <span class="admin-sidebar-tag">Coming next</span>
            </span>
        `;
    }

    const isActive = item.id === activePage;

    return `
        <a href="${item.href}" class="admin-sidebar-link${isActive ? " is-active" : ""}">
            ${escapeHtml(item.label)}
        </a>
    `;
}


/*
 * Wires the mobile hamburger toggle (#adminMenuToggle, expected in each
 * Admin page's topbar) plus the backdrop injected by renderAdminSidebar()
 * to open/close the off-canvas sidebar drawer below the ~1080px
 * breakpoint. Above that breakpoint the sidebar is always visible via CSS
 * and this toggle has no visible effect.
 */
export function setupAdminMobileMenu() {
    const shell = document.getElementById("adminShell");
    const menuToggle = document.getElementById("adminMenuToggle");
    const backdrop = document.getElementById("adminSidebarBackdrop");

    if (!shell || !menuToggle) {
        return;
    }

    function closeMenu() {
        shell.classList.remove("is-sidebar-open");
    }

    menuToggle.addEventListener("click", () => {
        shell.classList.toggle("is-sidebar-open");
    });

    if (backdrop) {
        backdrop.addEventListener("click", closeMenu);
    }

    shell.querySelectorAll(".admin-sidebar-link").forEach((link) => {
        link.addEventListener("click", closeMenu);
    });
}


function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
