using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class ParkingReservationRepository
    : IParkingReservationRepository
{
    private readonly ApplicationDbContext _context;

    public ParkingReservationRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetBookingByIdAsync(
        int bookingId)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(
                booking => booking.Id == bookingId);
    }

    public async Task<ParkingSlot?> GetParkingSlotByIdAsync(
        int parkingSlotId)
    {
        return await _context.ParkingSlots
            .Include(parkingSlot => parkingSlot.Event)
            .FirstOrDefaultAsync(
                parkingSlot =>
                    parkingSlot.Id == parkingSlotId);
    }

    public async Task<ParkingReservation?>
        GetByBookingIdAsync(int bookingId)
    {
        return await _context.ParkingReservations
            .Include(reservation =>
                reservation.ParkingSlot)
            .ThenInclude(parkingSlot =>
                parkingSlot.Event)
            .FirstOrDefaultAsync(
                reservation =>
                    reservation.BookingId == bookingId);
    }

    public async Task<bool>
        HasActiveReservationForSlotAsync(
            int parkingSlotId)
    {
        return await _context.ParkingReservations
            .AnyAsync(reservation =>
                reservation.ParkingSlotId ==
                    parkingSlotId &&
                reservation.IsActive);
    }

    public async Task AddAsync(
        ParkingReservation parkingReservation)
    {
        await _context.ParkingReservations
            .AddAsync(parkingReservation);
    }

    public async Task<int> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException
                    is SqlException sqlException &&
                  (sqlException.Number == 2601 ||
                   sqlException.Number == 2627))
        {
            throw new InvalidOperationException(
                "Parking reservation failed because the booking or parking slot is already reserved.",
                exception);
        }
    }
}