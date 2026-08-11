using System.Data;
using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _context;

    public BookingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IRepositoryTransaction>
        BeginSerializableTransactionAsync(
            CancellationToken cancellationToken = default)
    {
        var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        return new RepositoryTransaction(transaction);
    }

    public Task<Customer?> GetCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        return _context.Customers.FirstOrDefaultAsync(
            customer => customer.Id == customerId,
            cancellationToken);
    }

    public Task<Event?> GetEventAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        return _context.Events.FirstOrDefaultAsync(
            eventItem => eventItem.Id == eventId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Seat>>
        GetSeatsForUpdateAsync(
            int eventId,
            IReadOnlyCollection<int> seatIds,
            CancellationToken cancellationToken = default)
    {
        var eventSeats = await _context.Seats
            .FromSqlInterpolated(
                $"SELECT * FROM Seats WITH (UPDLOCK, HOLDLOCK) WHERE EventId = {eventId}")
            .OrderBy(seat => seat.Id)
            .ToListAsync(cancellationToken);

        return eventSeats
            .Where(seat => seatIds.Contains(seat.Id))
            .ToList();
    }

    public Task<ParkingSlot?> GetParkingSlotForUpdateAsync(
        int eventId,
        int parkingSlotId,
        CancellationToken cancellationToken = default)
    {
        return _context.ParkingSlots
            .FromSqlInterpolated(
                $"SELECT * FROM ParkingSlots WITH (UPDLOCK, HOLDLOCK) WHERE EventId = {eventId} AND Id = {parkingSlotId}")
            .Include(parkingSlot => parkingSlot.Event)
            .FirstOrDefaultAsync(
                parkingSlot => parkingSlot.Id == parkingSlotId,
                cancellationToken);
    }

    public Task AddBookingAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        return _context.Bookings
            .AddAsync(booking, cancellationToken)
            .AsTask();
    }

    public Task<Booking?> GetTrackedBookingAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return _context.Bookings
            .FromSqlInterpolated(
                $"SELECT * FROM Bookings WITH (UPDLOCK, HOLDLOCK) WHERE Id = {bookingId}")
            .Include(booking => booking.Customer)
            .Include(booking => booking.Event)
            .Include(booking => booking.BookingSeats)
                .ThenInclude(bookingSeat => bookingSeat.Seat)
            .Include(booking => booking.ParkingReservation)
                .ThenInclude(reservation => reservation!.ParkingSlot)
            .Include(booking => booking.Payment)
            .FirstOrDefaultAsync(
                booking => booking.Id == bookingId,
                cancellationToken);
    }

    public Task<Booking?> GetBookingDetailsAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return BookingDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                booking => booking.Id == bookingId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>>
        GetCustomerBookingsAsync(
            int customerId,
            CancellationToken cancellationToken = default)
    {
        return await BookingDetailsQuery()
            .AsNoTracking()
            .Where(booking => booking.CustomerId == customerId)
            .OrderByDescending(booking => booking.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetBookingsAsync(
        int? eventId,
        BookingStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = BookingDetailsQuery().AsNoTracking();

        if (eventId.HasValue)
        {
            query = query.Where(
                booking => booking.EventId == eventId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(
                booking => booking.Status == status.Value);
        }

        return await query
            .OrderByDescending(booking => booking.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>>
        GetExpiredPendingBookingIdsAsync(
            DateTime utcNow,
            CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.Status == BookingStatus.Pending &&
                booking.HoldExpiresAt.HasValue &&
                booking.HoldExpiresAt.Value <= utcNow)
            .OrderBy(booking => booking.Id)
            .Select(booking => booking.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>>
        GetCustomerPaymentsAsync(
            int customerId,
            CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(payment => payment.Booking)
            .Where(payment => payment.CustomerId == customerId)
            .OrderByDescending(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Payment?> GetPaymentDetailsAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        return _context.Payments
            .AsNoTracking()
            .Include(payment => payment.Customer)
            .Include(payment => payment.Booking)
                .ThenInclude(booking => booking.Event)
            .Include(payment => payment.Booking)
                .ThenInclude(booking => booking.BookingSeats)
                    .ThenInclude(bookingSeat => bookingSeat.Seat)
            .Include(payment => payment.Booking)
                .ThenInclude(booking => booking.ParkingReservation)
                    .ThenInclude(reservation => reservation!.ParkingSlot)
            .FirstOrDefaultAsync(
                payment => payment.Id == paymentId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>>
        GetCustomerNotificationsAsync(
            int customerId,
            CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.CustomerId == customerId)
            .OrderByDescending(notification =>
                notification.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Notification?>
        GetCustomerNotificationForUpdateAsync(
            int notificationId,
            int customerId,
            CancellationToken cancellationToken = default)
    {
        return _context.Notifications.FirstOrDefaultAsync(
            notification =>
                notification.Id == notificationId &&
                notification.CustomerId == customerId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<int>>
        GetCustomerIdsForEventAsync(
            int eventId,
            CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.EventId == eventId &&
                (booking.Status == BookingStatus.Pending ||
                 booking.Status == BookingStatus.Confirmed))
            .Select(booking => booking.CustomerId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasActiveFutureBookingAsync(
        int customerId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        return _context.Bookings.AnyAsync(
            booking =>
                booking.CustomerId == customerId &&
                booking.Event.StartDateTime > utcNow &&
                (booking.Status == BookingStatus.Pending ||
                 booking.Status == BookingStatus.Confirmed),
            cancellationToken);
    }

    public Task AddNotificationsAsync(
        IEnumerable<Notification> notifications,
        CancellationToken cancellationToken = default)
    {
        return _context.Notifications.AddRangeAsync(
            notifications,
            cancellationToken);
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException
                    is SqlException sqlException &&
                  (sqlException.Number == 2601 ||
                   sqlException.Number == 2627 ||
                   sqlException.Number == 1205))
        {
            throw new InvalidOperationException(
                "The booking could not be completed because a selected resource was claimed by another request.",
                exception);
        }
    }

    private IQueryable<Booking> BookingDetailsQuery()
    {
        return _context.Bookings
            .Include(booking => booking.Customer)
            .Include(booking => booking.Event)
            .Include(booking => booking.BookingSeats)
                .ThenInclude(bookingSeat => bookingSeat.Seat)
            .Include(booking => booking.ParkingReservation)
                .ThenInclude(reservation => reservation!.ParkingSlot)
            .Include(booking => booking.Payment);
    }
}
