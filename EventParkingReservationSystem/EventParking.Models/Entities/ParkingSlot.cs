using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Common;
using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class ParkingSlot : BaseEntity
{
    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    public string SlotNumber { get; set; } = string.Empty;

    public string? Zone { get; set; }

    public ParkingSlotStatus Status { get; set; }
        = ParkingSlotStatus.Available;
}
