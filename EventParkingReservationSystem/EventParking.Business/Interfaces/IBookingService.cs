using EventParking.Business.DTOs.Bookings;
using EventParking.Business.DTOs.Payments;
using EventParking.Models.Enums;

namespace EventParking.Business.Interfaces;

public interface IBookingService
{
    Task<BookingResponse> CreatePendingAsync(
        int customerId,
        CreateBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<BookingResponse?> GetByIdAsync(
        int bookingId,
        int requesterCustomerId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingResponse>> GetMyBookingsAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingResponse>> GetAllAsync(
        int? eventId,
        BookingStatus? status,
        CancellationToken cancellationToken = default);

    Task<HoldStatusResponse?> GetHoldStatusAsync(
        int bookingId,
        int requesterCustomerId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<bool> CancelPendingAsync(
        int bookingId,
        int requesterCustomerId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse?> GetPaymentAsync(
        int bookingId,
        int requesterCustomerId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse> SimulatePaymentAsync(
        int bookingId,
        int customerId,
        SimulatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentResponse>> GetMyPaymentsAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<byte[]?> GenerateReceiptPdfAsync(
        int paymentId,
        int requesterCustomerId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<int> ExpirePendingBookingsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveFutureBookingAsync(
        int customerId,
        CancellationToken cancellationToken = default);
}
