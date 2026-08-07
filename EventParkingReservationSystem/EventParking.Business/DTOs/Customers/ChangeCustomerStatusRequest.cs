using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace EventParking.Business.DTOs.Customers;

public class ChangeCustomerStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
