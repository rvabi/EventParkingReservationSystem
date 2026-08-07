using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventParking.Business.DTOs.Seats;

public class UpdateRowPriceRequest
{
    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "RowPrice cannot be negative.")]
    public decimal? RowPrice { get; set; }
}
