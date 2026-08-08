using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventParking.Models.Enums;

public enum ParkingSlotStatus
{
    Available = 1,
    Held = 2,
    Reserved = 3,
    Unavailable = 4
}