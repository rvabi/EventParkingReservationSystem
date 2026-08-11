using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventParking.Business.DTOs.Parking;

public class ParkingSlotResponse
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string SlotNumber { get; set; } = string.Empty;

    public string? Zone { get; set; }

    public decimal? FeeOverride { get; set; }

    public decimal EffectiveFee { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
