using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Common;

namespace EventParking.Models.Entities;

public class Event : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int VenueId { get; set; }

    public Venue Venue { get; set; } = null!;

    public int EventCategoryId { get; set; }

    public EventCategory EventCategory { get; set; } = null!;

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public decimal TicketPrice { get; set; }

    public decimal ParkingFee { get; set; }

    public int Capacity { get; set; }
}
