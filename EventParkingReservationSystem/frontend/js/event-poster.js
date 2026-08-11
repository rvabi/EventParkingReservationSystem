/*
 * Shared dynamic event poster builder used by both the Customer Events
 * list (events.js) and Event Details (event-details.js), so premium
 * concert-poster styling only has to be defined once and every Event
 * automatically shows its own real Name/Date/Time/Venue - no per-event
 * image generation or upload, no text baked into any image.
 *
 * Expected background asset: frontend/assets/images/event-concert-bg.png
 * (does not exist in this repo yet - see the README note in this file's
 * commit/report). Until it is added, the <img> below transparently falls
 * back to the only real image asset currently in the repo,
 * frontend/assets/images/login-hero-bg.png, via a native onerror handler -
 * no Base64 content or invented asset was created.
 */

const POSTER_IMAGE_PATH = "../assets/images/event-concert-bg.png";
const POSTER_FALLBACK_IMAGE_PATH = "../assets/images/login-hero-bg.png";

/*
 * sizeClass: "is-card" (Event list cards) | "is-hero" (Event Details page).
 * eventItem: real EventDto (name, startDateTime, endDateTime required).
 * venueName: already-resolved real Venue name, or null while loading.
 */
export function buildEventPosterHtml(eventItem, venueName, sizeClass) {
    const dateLabel = formatPosterDate(eventItem.startDateTime);
    const timeLabel = formatPosterTime(eventItem.startDateTime);

    return `
        <div class="event-poster ${sizeClass}">
            <img
                class="event-poster-bg"
                src="${POSTER_IMAGE_PATH}"
                alt=""
                onerror="this.onerror=null;this.src='${POSTER_FALLBACK_IMAGE_PATH}';">
            <div class="event-poster-overlay"></div>
            <div class="event-poster-content">
                <span class="event-poster-name">${escapeHtml(eventItem.name)}</span>
                <div class="event-poster-meta">
                    <span class="event-poster-meta-item">
                        <svg class="event-poster-meta-icon" viewBox="0 0 24 24" aria-hidden="true">
                            <rect x="3.5" y="5" width="17" height="15" rx="2" fill="none" stroke="currentColor" stroke-width="1.8" />
                            <line x1="3.5" y1="9.5" x2="20.5" y2="9.5" stroke="currentColor" stroke-width="1.8" />
                            <line x1="8" y1="3" x2="8" y2="7" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
                            <line x1="16" y1="3" x2="16" y2="7" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
                        </svg>
                        ${escapeHtml(dateLabel)}
                    </span>
                    <span class="event-poster-meta-item">
                        <svg class="event-poster-meta-icon" viewBox="0 0 24 24" aria-hidden="true">
                            <circle cx="12" cy="12" r="8.2" fill="none" stroke="currentColor" stroke-width="1.8" />
                            <path d="M12 7.5V12l3 2" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                        </svg>
                        ${escapeHtml(timeLabel)}
                    </span>
                </div>
                <span class="event-poster-venue">${escapeHtml((venueName || "").toUpperCase())}</span>
            </div>
        </div>
    `;
}


function formatPosterDate(value) {
    if (!value) {
        return "Date TBA";
    }

    const date = new Date(value);

    const day = date.getDate();

    const month = date
        .toLocaleDateString(undefined, { month: "short" })
        .toUpperCase();

    return `${day} ${month} ${date.getFullYear()}`;
}


function formatPosterTime(value) {
    if (!value) {
        return "";
    }

    return new Date(value).toLocaleTimeString(
        [],
        { hour: "2-digit", minute: "2-digit" }
    );
}


function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
