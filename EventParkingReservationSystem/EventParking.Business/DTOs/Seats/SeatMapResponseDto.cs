using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventParking.Business.DTOs.Seats;

public class SeatMapResponseDto
{
    public int EventId { get; set; }

    public int Capacity { get; set; }

    public decimal TicketPrice { get; set; }

    public int TotalSeats { get; set; }

    public List<SeatDto> Seats { get; set; } = new();
}
