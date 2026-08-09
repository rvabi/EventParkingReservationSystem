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

function createSeatButton(seat, { mode, selectedSeatIds, onSeatClick }) {
    const button = document.createElement("button");
    button.type = "button";

    const statusName = seatStatusName(seat.status);

    button.className = `seat-btn seat-status-${statusName.toLowerCase()}`;
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

    button.textContent = seat.seatNumber || "";

    const priceText =
        typeof seat.price === "number"
            ? `, LKR ${seat.price.toFixed(2)}`
            : "";

    button.setAttribute(
        "aria-label",
        `Seat ${seat.seatNumber}, Row ${seat.rowLabel || "-"}, ${statusName}${priceText}`
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
 * Ground/fan layout: distributes the real rows across three visual bands
 * (front/mid/back) and applies a per-seat curve computed from column
 * position within its row. This is presentation-only geometry derived at
 * render time from real row/column data - no coordinates are invented or
 * persisted anywhere.
 */
function renderGroundLayout(wrapper, rows, options) {
    const fan = document.createElement("div");
    fan.className = "seat-ground-fan";

    const totalRows = rows.length;
    const bandWidths = [58, 76, 94];
    const bandArc = [12, 22, 32];

    const bands = [[], [], []];

    rows.forEach((row, index) => {
        const bandIndex =
            totalRows <= 1
                ? 0
                : Math.min(2, Math.floor((index * 3) / totalRows));

        bands[bandIndex].push(row);
    });

    bands.forEach((bandRows, bandIndex) => {
        if (!bandRows.length) {
            return;
        }

        const bandEl = document.createElement("div");
        bandEl.className = `seat-ground-band seat-ground-band-${bandIndex + 1}`;
        bandEl.style.maxWidth = `${bandWidths[bandIndex]}%`;

        bandRows.forEach((row) => {
            const rowEl = document.createElement("div");
            rowEl.className = "seat-row seat-row-ground";

            const label = document.createElement("span");
            label.className = "seat-row-label";
            label.textContent = row.rowLabel || "-";
            rowEl.appendChild(label);

            const seatsEl = document.createElement("div");
            seatsEl.className = "seat-row-seats seat-row-seats-ground";

            const count = row.seats.length;
            const arc = bandArc[bandIndex];

            row.seats.forEach((seat, seatIndex) => {
                const button = createSeatButton(seat, options);

                const t =
                    count > 1
                        ? (seatIndex - (count - 1) / 2) / ((count - 1) / 2)
                        : 0;

                const yOffset = arc * (1 - Math.cos(t * (Math.PI / 2.4)));
                const rotate = t * 5;

                button.style.transform =
                    `translateY(${yOffset.toFixed(1)}px) rotate(${rotate.toFixed(1)}deg)`;

                seatsEl.appendChild(button);
            });

            rowEl.appendChild(seatsEl);
            bandEl.appendChild(rowEl);
        });

        fan.appendChild(bandEl);
    });

    wrapper.appendChild(fan);
}

/*
 * container: element to render into (its content is replaced).
 * options.seats: array of SeatDto-shaped objects (id, seatNumber,
 *   rowLabel, columnNumber, status, price).
 * options.layout: "square" | "ground".
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

    const wrapper = document.createElement("div");
    wrapper.className = `seat-map seat-map-${layout === "ground" ? "ground" : "square"}`;

    const stage = document.createElement("div");
    stage.className = "seat-stage";
    stage.textContent = "STAGE";
    wrapper.appendChild(stage);

    const renderOptions = { mode, selectedSeatIds, onSeatClick };

    if (layout === "ground") {
        renderGroundLayout(wrapper, rows, renderOptions);
    } else {
        renderSquareLayout(wrapper, rows, renderOptions);
    }

    container.appendChild(wrapper);
}
