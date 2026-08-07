using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Common;
using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class Booking : BaseEntity
{
    public string BookingNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    public BookingStatus Status { get; set; }
        = BookingStatus.Pending;

    public DateTime? HoldExpiresAt { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime? CancelledAt { get; set; }

    public ICollection<BookingSeat> BookingSeats { get; set; }
        = new List<BookingSeat>();
}
