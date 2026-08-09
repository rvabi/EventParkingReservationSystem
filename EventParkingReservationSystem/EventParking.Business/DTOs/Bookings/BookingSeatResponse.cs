namespace EventParking.Business.DTOs.Bookings;

public sealed class BookingSeatResponse
{
    public int SeatId { get; init; }

    public string SeatNumber { get; init; } = string.Empty;

    public decimal UnitPriceAtBooking { get; init; }
}
