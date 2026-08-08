using System.ComponentModel.DataAnnotations;
using EventParking.Models.Enums;

namespace EventParking.Business.DTOs.VenueFacilities;

public class CreateVenueFacilityRequest
{
    [Range(1, int.MaxValue)]
    public int VenueId { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public FacilityType FacilityType { get; set; }

    [StringLength(100)]
    public string? Zone { get; set; }

    [StringLength(100)]
    public string? Floor { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsAccessible { get; set; }

    [Required]
    public FacilityStatus Status { get; set; }

    [StringLength(500)]
    public string? Directions { get; set; }
}