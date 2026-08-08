using EventParking.Business.DTOs.Parking;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class ParkingReservationService
    : IParkingReservationService
{
    private readonly IParkingReservationRepository
        _parkingReservationRepository;

    public ParkingReservationService(
        IParkingReservationRepository
            parkingReservationRepository)
    {
        _parkingReservationRepository =
            parkingReservationRepository;
    }

    public async Task<ParkingReservationResponse>
        ReserveAsync(
            int bookingId,
            int parkingSlotId)
    {
        if (bookingId <= 0)
        {
            throw new ArgumentException(
                "A valid booking ID is required.",
                nameof(bookingId));
        }

        if (parkingSlotId <= 0)
        {
            throw new ArgumentException(
                "A valid parking slot ID is required.",
                nameof(parkingSlotId));
        }

        var booking =
            await _parkingReservationRepository
                .GetBookingByIdAsync(bookingId);

        if (booking is null)
        {
            throw new InvalidOperationException(
                "Booking not found.");
        }

        if (booking.Status != BookingStatus.Pending &&
            booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "Parking cannot be reserved for a cancelled or expired booking.");
        }

        var existingReservation =
            await _parkingReservationRepository
                .GetByBookingIdAsync(bookingId);

        if (existingReservation is not null)
        {
            throw new InvalidOperationException(
                "This booking already has a parking reservation.");
        }

        var parkingSlot =
            await _parkingReservationRepository
                .GetParkingSlotByIdAsync(
                    parkingSlotId);

        if (parkingSlot is null)
        {
            throw new InvalidOperationException(
                "Parking slot not found.");
        }

        if (parkingSlot.EventId != booking.EventId)
        {
            throw new InvalidOperationException(
                "The selected parking slot does not belong to the booked event.");
        }

        if (parkingSlot.Status !=
            ParkingSlotStatus.Available)
        {
            throw new InvalidOperationException(
                "The selected parking slot is no longer available.");
        }

        var alreadyReserved =
            await _parkingReservationRepository
                .HasActiveReservationForSlotAsync(
                    parkingSlotId);

        if (alreadyReserved)
        {
            throw new InvalidOperationException(
                "The selected parking slot is already reserved.");
        }

        if (parkingSlot.Event is null)
        {
            throw new InvalidOperationException(
                "Parking fee information could not be loaded.");
        }

        var feeAtReservation =
            parkingSlot.FeeOverride
            ?? parkingSlot.Event.ParkingFee;

        if (feeAtReservation < 0)
        {
            throw new InvalidOperationException(
                "Parking fee cannot be negative.");
        }

        var now = DateTime.UtcNow;

        var reservation =
            new ParkingReservation
            {
                BookingId = booking.Id,
                ParkingSlotId = parkingSlot.Id,
                FeeAtReservation =
                    feeAtReservation,
                IsActive = true,
                CreatedAt = now
            };

        parkingSlot.Status =
            booking.Status == BookingStatus.Confirmed
                ? ParkingSlotStatus.Reserved
                : ParkingSlotStatus.Held;

        parkingSlot.UpdatedAt = now;

        await _parkingReservationRepository
            .AddAsync(reservation);

        await _parkingReservationRepository
            .SaveChangesAsync();

        reservation.ParkingSlot = parkingSlot;

        return MapToResponse(reservation);
    }

    public async Task<ParkingReservationResponse?>
        ConfirmAsync(int bookingId)
    {
        if (bookingId <= 0)
        {
            return null;
        }

        var reservation =
            await _parkingReservationRepository
                .GetByBookingIdAsync(bookingId);

        if (reservation is null ||
            !reservation.IsActive)
        {
            return null;
        }

        if (reservation.ParkingSlot.Status ==
            ParkingSlotStatus.Reserved)
        {
            return MapToResponse(reservation);
        }

        if (reservation.ParkingSlot.Status !=
            ParkingSlotStatus.Held)
        {
            throw new InvalidOperationException(
                "Only a held parking slot can be confirmed.");
        }

        reservation.ParkingSlot.Status =
            ParkingSlotStatus.Reserved;

        reservation.ParkingSlot.UpdatedAt =
            DateTime.UtcNow;

        reservation.UpdatedAt =
            DateTime.UtcNow;

        await _parkingReservationRepository
            .SaveChangesAsync();

        return MapToResponse(reservation);
    }

    public async Task<bool> ReleaseAsync(
        int bookingId)
    {
        if (bookingId <= 0)
        {
            return false;
        }

        var reservation =
            await _parkingReservationRepository
                .GetByBookingIdAsync(bookingId);

        if (reservation is null ||
            !reservation.IsActive)
        {
            return false;
        }

        var now = DateTime.UtcNow;

        reservation.IsActive = false;
        reservation.UpdatedAt = now;

        reservation.ParkingSlot.Status =
            ParkingSlotStatus.Available;

        reservation.ParkingSlot.UpdatedAt = now;

        await _parkingReservationRepository
            .SaveChangesAsync();

        return true;
    }

    public async Task<ParkingReservationResponse?>
        GetByBookingIdAsync(int bookingId)
    {
        if (bookingId <= 0)
        {
            return null;
        }

        var reservation =
            await _parkingReservationRepository
                .GetByBookingIdAsync(bookingId);

        return reservation is null
            ? null
            : MapToResponse(reservation);
    }

    private static ParkingReservationResponse
        MapToResponse(
            ParkingReservation reservation)
    {
        return new ParkingReservationResponse
        {
            Id = reservation.Id,
            BookingId = reservation.BookingId,
            ParkingSlotId =
                reservation.ParkingSlotId,
            SlotNumber =
                reservation.ParkingSlot.SlotNumber,
            Zone =
                reservation.ParkingSlot.Zone,
            FeeAtReservation =
                reservation.FeeAtReservation,
            IsActive =
                reservation.IsActive,
            SlotStatus =
                reservation.ParkingSlot.Status
                    .ToString(),
            CreatedAt =
                reservation.CreatedAt,
            UpdatedAt =
                reservation.UpdatedAt
        };
    }
}
