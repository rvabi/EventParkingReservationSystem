using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Common;

namespace EventParking.Models.Entities;

public class BookingSeat : BaseEntity
{
    public int BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    public int SeatId { get; set; }

    public Seat Seat { get; set; } = null!;
}
