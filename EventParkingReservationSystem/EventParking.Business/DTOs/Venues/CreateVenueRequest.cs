using System.ComponentModel.DataAnnotations;

namespace EventParking.Business.DTOs.Venues;

public class CreateVenueRequest
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int TotalCapacity { get; set; }
}