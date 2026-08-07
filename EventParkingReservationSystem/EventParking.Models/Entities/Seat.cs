using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Common;
using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class Seat : BaseEntity
{
    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    public string SeatNumber { get; set; } = string.Empty;

    public string? RowLabel { get; set; }

    public int? ColumnNumber { get; set; }

    public string? SeatType { get; set; }

    public decimal? PriceOverride { get; set; }

    public SeatStatus Status { get; set; } = SeatStatus.Available;

    public ICollection<BookingSeat> BookingSeats { get; set; }
        = new List<BookingSeat>();
}
