namespace EventParking.Business.DTOs.VenueFacilities;

public class VenueFacilityDto
{
    public int Id { get; set; }

    public int VenueId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FacilityType { get; set; } = string.Empty;

    public string? Zone { get; set; }

    public string? Floor { get; set; }

    public string? Description { get; set; }

    public bool IsAccessible { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Directions { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}