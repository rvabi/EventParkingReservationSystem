/*
 * Shared seat map renderer used by both the customer Seat Selection page
 * and the Admin Seat Management page, so geometry logic is defined once.
 *
 * SeatStatus values below mirror EventParking.Models.Enums.SeatStatus
 * exactly (Available = 1, Held = 2, Booked = 3, Unavailable = 4). The API
 * has no JsonStringEnumConverter registered, so seat.status arrives as a
 * number, not a string - do not compare it against status name strings.
 *
 * "Selected" is a frontend-only visual state (seat-selected class) and is
 * never sent to or stored by the backend.
 */

export const SEAT_STATUS = {
    AVAILABLE: 1,
    HELD: 2,
    BOOKED: 3,
    UNAVAILABLE: 4
};

const SEAT_STATUS_NAME = {
    1: "Available",
    2: "Held",
    3: "Booked",
    4: "Unavailable"
};

export function seatStatusName(status) {
    return SEAT_STATUS_NAME[status] || "Unknown";
}

/*
 * Maps the persisted Event.SeatingLayoutType string (as returned by
 * EventDto - "StraightRows" or "CircularArena", see
 * EventParking.Models.Enums.SeatingLayoutType) to the layout keyword this
 * renderer understands ("square" | "circular"). Shared by manage-seats.js
 * (admin) and seat-selection.js (customer) so both read the same persisted
 * field the same way instead of duplicating the mapping.
 */
export function layoutModeFromSeatingLayoutType(seatingLayoutType) {
    return seatingLayoutType === "CircularArena" ? "circular" : "square";
}

/*
 * Converts a bijective base-26 row label (A, B, ... Z, AA, AB, ...) back
 * into its generation index, matching SeatService.GetRowLabel exactly, so
 * rows sort in real generation order rather than plain string order
 * (which would incorrectly place "AA" before "B").
 */
function rowLabelToIndex(label) {
    let result = 0;

    for (const char of String(label || "").toUpperCase()) {
        if (char < "A" || char > "Z") {
            continue;
        }

        result = result * 26 + (char.charCodeAt(0) - 64);
    }

    return result;
}

export function groupSeatsByRow(seats) {
    const rowsMap = new Map();

    seats.forEach((seat) => {
        const rowLabel = seat.rowLabel || "";

        if (!rowsMap.has(rowLabel)) {
            rowsMap.set(rowLabel, []);
        }

        rowsMap.get(rowLabel).push(seat);
    });

    const rows = Array.from(rowsMap.entries()).map(([rowLabel, rowSeats]) => ({
        rowLabel,
        seats: rowSeats
            .slice()
            .sort((a, b) => (a.columnNumber || 0) - (b.columnNumber || 0))
    }));

    rows.sort(
        (a, b) => rowLabelToIndex(a.rowLabel) - rowLabelToIndex(b.rowLabel)
    );

    return rows;
}

function createSeatButton(seat, { mode, selectedSeatIds, onSeatClick, shape, unitLabel, hideLabel }) {
    const button = document.createElement("button");
    button.type = "button";

    const statusName = seatStatusName(seat.status);

    button.className = `seat-btn seat-status-${statusName.toLowerCase()}`;

    if (shape === "circle") {
        button.classList.add("seat-btn-circle");
    }

    if (hideLabel) {
        button.classList.add("seat-btn-dot");
    }

    button.dataset.seatId = String(seat.id);

    const isSelected = Boolean(selectedSeatIds && selectedSeatIds.has(seat.id));

    if (isSelected) {
        button.classList.add("seat-selected");
    }

    const interactive =
        mode === "admin"
            ? seat.status === SEAT_STATUS.AVAILABLE || seat.status === SEAT_STATUS.UNAVAILABLE
            : seat.status === SEAT_STATUS.AVAILABLE;

    button.disabled = !interactive;

    /*
     * Customer CircularArena seats render as plain dots (no number text) -
     * with hundreds of seats packed around concentric rings, per-seat
     * labels overlap and become unclickable. The seat number is still
     * available via aria-label/title for accessibility and hover, and the
     * dedicated Ring/Seat dropdown chooser (seat-selection.js) is the
     * precise way to pick a specific seat number in this layout.
     */
    button.textContent = hideLabel ? "" : (seat.seatNumber || "");

    const priceText =
        typeof seat.price === "number"
            ? `, LKR ${seat.price.toFixed(2)}`
            : "";

    button.setAttribute(
        "aria-label",
        `Seat ${seat.seatNumber}, ${unitLabel || "Row"} ${seat.rowLabel || "-"}, ${statusName}${priceText}`
    );

    button.title = `${seat.seatNumber} - ${statusName}${priceText}`;

    if (interactive && typeof onSeatClick === "function") {
        button.addEventListener("click", () => onSeatClick(seat, button));
    }

    return button;
}

function renderSquareLayout(wrapper, rows, options) {
    const grid = document.createElement("div");
    grid.className = "seat-square-grid";

    rows.forEach((row) => {
        const rowEl = document.createElement("div");
        rowEl.className = "seat-row";

        const label = document.createElement("span");
        label.className = "seat-row-label";
        label.textContent = row.rowLabel || "-";
        rowEl.appendChild(label);

        const seatsEl = document.createElement("div");
        seatsEl.className = "seat-row-seats";

        row.seats.forEach((seat) => {
            seatsEl.appendChild(createSeatButton(seat, options));
        });

        rowEl.appendChild(seatsEl);
        grid.appendChild(rowEl);
    });

    wrapper.appendChild(grid);
}

/*
 * True 360-degree circular arena renderer. Rows returned by
 * groupSeatsByRow are already sorted in real backend generation order
 * (A, B, C, ...), which - because GenerateSeatMapAsync assigns row labels
 * in the order rows were submitted - lines up exactly with "Ring A =
 * nearest center, Ring B = next, ..." from the admin's Number of Rings
 * input. Each ring is a concentric circle of the given radius; seats
 * within a ring are spaced evenly around the full 360 degrees using
 * simple trigonometry. This is presentation-only geometry computed at
 * render time from real row/column data - nothing here is persisted.
 *
 * container is passed in (not just the wrapper) so the renderer can
 * measure real available width after layout and scale the whole arena
 * down to fit small screens without causing page-level horizontal
 * overflow - the seat-map-container's own overflow-x:auto remains as a
 * fallback if an unusually large ring count still doesn't fit.
 */
function renderCircularLayout(container, wrapper, rows, options) {
    const STAGE_RADIUS = 56;
    const RING_GAP = 44;
    const SEAT_SIZE = 26;
    const OUTER_PADDING = 14;

    const arenaHolder = document.createElement("div");
    arenaHolder.className = "seat-arena-holder";

    const arena = document.createElement("div");
    arena.className = "seat-arena";

    const ringCount = rows.length;
    const outerRadius = STAGE_RADIUS + RING_GAP * ringCount;
    const containerRadius = outerRadius + SEAT_SIZE / 2 + OUTER_PADDING;
    const containerSize = containerRadius * 2;

    arena.style.width = `${containerSize}px`;
    arena.style.height = `${containerSize}px`;

    const stage = document.createElement("div");
    stage.className = "seat-arena-stage";
    stage.style.width = `${STAGE_RADIUS * 2}px`;
    stage.style.height = `${STAGE_RADIUS * 2}px`;
    stage.textContent = "EVENT / STAGE";
    arena.appendChild(stage);

    rows.forEach((row, ringIndex) => {
        const radius = STAGE_RADIUS + RING_GAP * (ringIndex + 1);
        const seatCount = row.seats.length;

        if (seatCount === 0) {
            return;
        }

        const angleStep = 360 / seatCount;

        row.seats.forEach((seat, seatIndex) => {
            const angleDeg = angleStep * seatIndex - 90;
            const angleRad = (angleDeg * Math.PI) / 180;

            const x = radius * Math.cos(angleRad);
            const y = radius * Math.sin(angleRad);

            const button = createSeatButton(seat, {
                ...options,
                shape: "circle",
                unitLabel: "Ring",
                hideLabel: options.mode === "customer"
            });

            button.classList.add("seat-btn-arena");
            button.style.left = `calc(50% + ${x.toFixed(2)}px)`;
            button.style.top = `calc(50% + ${y.toFixed(2)}px)`;

            arena.appendChild(button);
        });
    });

    arenaHolder.appendChild(arena);
    wrapper.appendChild(arenaHolder);

    /*
     * Deferred to a frame after the wrapper has actually been attached to
     * `container` (renderSeatMap appends `wrapper` to `container`
     * immediately after this function returns, which happens
     * synchronously before the next animation frame), so container.
     * clientWidth reflects the real, already-laid-out available width.
     */
    requestAnimationFrame(() => {
        const availableWidth = container.clientWidth;

        if (availableWidth > 0 && containerSize > availableWidth) {
            const scale = Math.max(0.55, availableWidth / containerSize);

            arena.style.transform = `scale(${scale})`;
            arenaHolder.style.height = `${containerSize * scale}px`;
        }
    });
}

/*
 * container: element to render into (its content is replaced).
 * options.seats: array of SeatDto-shaped objects (id, seatNumber,
 *   rowLabel, columnNumber, status, price).
 * options.layout: "square" | "circular".
 * options.mode: "customer" | "admin" - controls which statuses are
 *   clickable (customer: Available only; admin: Available/Unavailable
 *   toggle; Held/Booked are always locked in both modes).
 * options.selectedSeatIds: Set of seat ids currently selected
 *   (customer-only visual state).
 * options.onSeatClick(seat, buttonEl): invoked on interactive seat click.
 *   The renderer never calls the API itself - callers own that.
 */
export function renderSeatMap(container, options = {}) {
    const {
        seats = [],
        layout = "square",
        mode = "customer",
        selectedSeatIds = new Set(),
        onSeatClick = null
    } = options;

    container.innerHTML = "";

    if (!seats.length) {
        const empty = document.createElement("p");
        empty.className = "seat-map-empty";
        empty.textContent = "No seats have been generated for this event yet.";
        container.appendChild(empty);
        return;
    }

    const rows = groupSeatsByRow(seats);
    const isCircular = layout === "circular";

    const wrapper = document.createElement("div");
    wrapper.className = `seat-map seat-map-${isCircular ? "circular" : "square"}`;

    const renderOptions = { mode, selectedSeatIds, onSeatClick, unitLabel: isCircular ? "Ring" : "Row" };

    if (isCircular) {
        renderCircularLayout(container, wrapper, rows, renderOptions);
    } else {
        const stage = document.createElement("div");
        stage.className = "seat-stage";
        stage.textContent = "STAGE";
        wrapper.appendChild(stage);

        renderSquareLayout(wrapper, rows, renderOptions);
    }

    container.appendChild(wrapper);
}
