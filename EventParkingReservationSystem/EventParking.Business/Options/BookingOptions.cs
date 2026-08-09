namespace EventParking.Business.Options;

public sealed class BookingOptions
{
    public const string SectionName = "Booking";

    public int HoldMinutes { get; init; } = 15;

    public int ExpiryScanSeconds { get; init; } = 60;
}
