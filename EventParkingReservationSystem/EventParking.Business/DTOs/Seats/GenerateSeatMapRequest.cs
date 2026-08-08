using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventParking.Business.DTOs.Seats;

public class GenerateSeatMapRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one row is required.")]
    public List<RowDefinitionRequest> Rows { get; set; } = new();
}

public class RowDefinitionRequest
{
    [Range(1, 1000, ErrorMessage = "SeatCount must be at least 1.")]
    public int SeatCount { get; set; }

    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "RowPrice cannot be negative.")]
    public decimal? RowPrice { get; set; }
}
