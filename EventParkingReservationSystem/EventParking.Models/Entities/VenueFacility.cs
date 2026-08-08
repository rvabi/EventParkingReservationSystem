using EventParking.Models.Common;
using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class VenueFacility : BaseEntity
{
    public int VenueId { get; set; }

    public Venue Venue { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public FacilityType FacilityType { get; set; }

    public string? Zone { get; set; }

    public string? Floor { get; set; }

    public string? Description { get; set; }

    public bool IsAccessible { get; set; }

    public FacilityStatus Status { get; set; }

    public string? Directions { get; set; }
}