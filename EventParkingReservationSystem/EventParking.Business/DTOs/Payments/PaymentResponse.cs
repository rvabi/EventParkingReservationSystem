namespace EventParking.Business.DTOs.Payments;

public sealed class PaymentResponse
{
    public int Id { get; init; }

    public int BookingId { get; init; }

    public string BookingNumber { get; init; } = string.Empty;

    public int CustomerId { get; init; }

    public decimal Amount { get; init; }

    public string BookingStatus { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string? TransactionReference { get; init; }

    public DateTime? PaidAt { get; init; }

    public DateTime CreatedAt { get; init; }
}
