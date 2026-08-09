# Booking, Payment, Hold Expiry and Notifications

This backend module implements Team Member 5's allocation from the Event &
Parking Reservation System BRD v1.3 and the approved person-wise assignment.
It contains no frontend code.

## Frozen decisions

- A booking contains one event, at least one seat, and zero or one parking
  slot.
- A new booking is `Pending` for 15 minutes.
- The expiry service scans every 60 seconds.
- Only an unpaid, unexpired `Pending` booking can be cancelled.
- A booking cannot be cancelled after a successful simulated payment.
- Failed simulated payment attempts update the one Payment row and may be
  retried while the booking hold remains active.
- There is no real gateway or refund processing.
- Receipt downloads are limited to the payment owner or an Administrator and
  only for a completed payment.

## Transaction design

Booking creation, payment, cancellation, and each expiry operation run in a
SQL Server `Serializable` transaction. Seat and parking inventory is selected
with `UPDLOCK` and `HOLDLOCK` before its status is checked. The API does not
trust the previously displayed frontend availability.

Booking creation performs the following work in one transaction:

1. Validate the authenticated customer is active and email-verified.
2. Validate the event is in the future.
3. Lock and validate all requested event seats.
4. Optionally lock and validate the event parking slot.
5. Snapshot every seat price and the optional parking fee.
6. Create the Pending booking and related rows.
7. Mark inventory Held and set `HoldExpiresAt`.
8. Generate `BKG-{year}-{database ID padded to six digits}`.
9. Create the booking-created notification.

Payment locks and rechecks the booking. Success updates the existing/new
Payment row, confirms the booking, converts held seats to Booked and optional
parking to Reserved, and creates a notification in one transaction. Failure
updates the same Payment row to Failed and leaves the booking Pending.

Expiry locks and rechecks every candidate booking before changing it to
Expired. It releases held inventory and creates one expiry notification.
Rechecking the status inside the transaction makes the operation idempotent
and protects payment from racing expiry.

## API endpoints

| Method | Route | Access | Behavior |
|---|---|---|---|
| POST | `/api/bookings` | Customer | Create a Pending booking and atomic hold. |
| GET | `/api/bookings/{id}` | Owner/Admin | Booking details. |
| GET | `/api/bookings/my-bookings` | Customer | Own booking history. |
| GET | `/api/bookings` | Admin | Filter by `eventId` and/or `status`. |
| GET | `/api/bookings/{id}/hold-status` | Owner/Admin | Status and remaining seconds. |
| DELETE | `/api/bookings/{id}` | Owner/Admin | Cancel only an unpaid Pending booking. |
| GET | `/api/bookings/{id}/payment` | Owner/Admin | Amount due and current payment status. |
| POST | `/api/bookings/{id}/payment` | Customer | Simulate payment success/failure. |
| GET | `/api/payments/my-payments` | Customer | Own payment history. |
| GET | `/api/payments/{id}/receipt` | Owner/Admin | Download completed-payment PDF receipt. |
| GET | `/api/notifications/my-notifications` | Customer | Own notifications, newest first. |
| PUT | `/api/notifications/{id}/read` | Customer | Mark own notification read. |

### Create booking example

```json
{
  "eventId": 1,
  "seatIds": [1, 2],
  "parkingSlotId": 3
}
```

Use `null` or omit `parkingSlotId` to book without parking.

### Simulated payment examples

```json
{
  "simulateSuccess": true
}
```

```json
{
  "simulateSuccess": false
}
```

## Configuration

```json
"Booking": {
  "HoldMinutes": 15,
  "ExpiryScanSeconds": 60
}
```

All persisted and returned timestamps are UTC. The frontend should convert
them for display.

## Migration

`20260809093000_AddBookingPriceSnapshot` adds
`BookingSeats.UnitPriceAtBooking`, backfills any existing rows from the seat
override or event ticket price, and adds a non-negative check constraint.

## Internal handoff contracts

- Customer module: call `IBookingService.HasActiveFutureBookingAsync` before
  deactivating an account.
- Event module: call `INotificationService.NotifyEventUpdatedAsync` after a
  successful relevant event update.
- Food Court module: call `INotificationService.NotifyFoodReadyAsync` when an
  order enters `ReadyForPickup`.
- No public endpoint creates notifications.

## Required verification

Run the automated test suite and then use two simultaneous Swagger clients to
try the same seat/parking booking. Only one request may succeed. Also race a
successful payment against the expiry scan for the same booking; the final
state must be either Confirmed with reserved resources or Expired with released
resources, never a mixture.
