namespace EventParking.Business.DTOs.Bookings;

public sealed class BookingParkingResponse
{
    public int ParkingSlotId { get; init; }

    public string SlotNumber { get; init; } = string.Empty;

    public string? Zone { get; init; }

    public decimal FeeAtReservation { get; init; }
}
