using System.ComponentModel.DataAnnotations;

using EventParking.Models.Enums;

namespace EventParking.Business.DTOs.Events;

public class UpdateEventRequest
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

    /*
     * Nullable so the current frontend (which does not send this field
     * yet, pending the Create Event wizard) keeps working: the controller
     * falls back to the event's existing SeatingLayoutType when this is
     * omitted, rather than silently resetting it to StraightRows on every
     * edit made from the old form. When a value IS supplied, EnumDataType
     * still rejects anything that isn't a defined SeatingLayoutType member.
     */
    [EnumDataType(typeof(SeatingLayoutType))]
    public SeatingLayoutType? SeatingLayoutType { get; set; }
}