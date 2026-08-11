namespace EventParking.Business.DTOs.Bookings;

public sealed class HoldStatusResponse
{
    public int BookingId { get; init; }

    public string BookingNumber { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime? HoldExpiresAt { get; init; }

    public int RemainingSeconds { get; init; }

    public bool CanPay { get; init; }
}
