using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.DataAccess.Interfaces;

public interface IBookingRepository
{
    Task<IRepositoryTransaction> BeginSerializableTransactionAsync(
        CancellationToken cancellationToken = default);

    Task<Customer?> GetCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<Event?> GetEventAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Seat>> GetSeatsForUpdateAsync(
        int eventId,
        IReadOnlyCollection<int> seatIds,
        CancellationToken cancellationToken = default);

    Task<ParkingSlot?> GetParkingSlotForUpdateAsync(
        int eventId,
        int parkingSlotId,
        CancellationToken cancellationToken = default);

    Task AddBookingAsync(
        Booking booking,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetTrackedBookingAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetBookingDetailsAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetCustomerBookingsAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetBookingsAsync(
        int? eventId,
        BookingStatus? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetExpiredPendingBookingIdsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetCustomerPaymentsAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetPaymentDetailsAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetCustomerNotificationsAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<Notification?> GetCustomerNotificationForUpdateAsync(
        int notificationId,
        int customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetCustomerIdsForEventAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveFutureBookingAsync(
        int customerId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task AddNotificationsAsync(
        IEnumerable<Notification> notifications,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
