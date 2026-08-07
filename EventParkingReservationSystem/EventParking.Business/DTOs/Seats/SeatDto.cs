using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Enums;

namespace EventParking.Business.DTOs.Seats;

public class SeatDto
{
    public int Id { get; set; }

    public string SeatNumber { get; set; } = string.Empty;

    public string? RowLabel { get; set; }

    public int? ColumnNumber { get; set; }

    public SeatStatus Status { get; set; }

    public decimal Price { get; set; }
}
