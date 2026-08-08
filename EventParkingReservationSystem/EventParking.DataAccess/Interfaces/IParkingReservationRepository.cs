using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IParkingReservationRepository
{
    Task<Booking?> GetBookingByIdAsync(
        int bookingId);

    Task<ParkingSlot?> GetParkingSlotByIdAsync(
        int parkingSlotId);

    Task<ParkingReservation?> GetByBookingIdAsync(
        int bookingId);

    Task<bool> HasActiveReservationForSlotAsync(
        int parkingSlotId);

    Task AddAsync(
        ParkingReservation parkingReservation);

    Task<int> SaveChangesAsync();
}