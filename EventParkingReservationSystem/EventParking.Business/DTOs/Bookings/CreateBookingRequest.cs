namespace EventParking.Business.DTOs.Bookings;

public sealed class CreateBookingRequest
{
    public int EventId { get; init; }

    public IReadOnlyList<int> SeatIds { get; init; }
        = Array.Empty<int>();

    public int? ParkingSlotId { get; init; }
}
