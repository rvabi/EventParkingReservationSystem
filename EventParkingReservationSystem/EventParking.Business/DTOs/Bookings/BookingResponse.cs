namespace EventParking.Business.DTOs.Bookings;

public sealed class BookingResponse
{
    public int Id { get; init; }

    public string BookingNumber { get; init; } = string.Empty;

    public int CustomerId { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public int EventId { get; init; }

    public string EventName { get; init; } = string.Empty;

    public DateTime EventStartDateTime { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime? HoldExpiresAt { get; init; }

    public int RemainingHoldSeconds { get; init; }

    public decimal SeatTotal { get; init; }

    public decimal ParkingTotal { get; init; }

    public decimal TotalAmount { get; init; }

    public IReadOnlyList<BookingSeatResponse> Seats { get; init; }
        = Array.Empty<BookingSeatResponse>();

    public BookingParkingResponse? Parking { get; init; }

    public string? PaymentStatus { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? CancelledAt { get; init; }
}
