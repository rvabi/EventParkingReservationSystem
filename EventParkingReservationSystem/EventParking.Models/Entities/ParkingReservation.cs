using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Common;

namespace EventParking.Models.Entities;

public class ParkingReservation : BaseEntity
{
    public int BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    public int ParkingSlotId { get; set; }

    public ParkingSlot ParkingSlot { get; set; } = null!;

    public decimal FeeAtReservation { get; set; }
}
