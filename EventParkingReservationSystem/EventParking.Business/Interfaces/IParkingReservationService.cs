using EventParking.Business.DTOs.Parking;

namespace EventParking.Business.Interfaces;

public interface IParkingReservationService
{
    Task<ParkingReservationResponse> ReserveAsync(
        int bookingId,
        int parkingSlotId);

    Task<ParkingReservationResponse?> ConfirmAsync(
        int bookingId);

    Task<bool> ReleaseAsync(
        int bookingId);

    Task<ParkingReservationResponse?> GetByBookingIdAsync(
        int bookingId);
}