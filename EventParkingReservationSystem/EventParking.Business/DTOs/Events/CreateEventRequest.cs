using System.ComponentModel.DataAnnotations;

namespace EventParking.Business.DTOs.Events;

public class CreateEventRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int VenueId { get; set; }

    [Range(1, int.MaxValue)]
    public int EventCategoryId { get; set; }

    [Required]
    public DateTime StartDateTime { get; set; }

    [Required]
    public DateTime EndDateTime { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal TicketPrice { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal ParkingFee { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
}