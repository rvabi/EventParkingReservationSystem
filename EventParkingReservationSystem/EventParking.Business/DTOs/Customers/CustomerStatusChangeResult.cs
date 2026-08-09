using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventParking.Business.DTOs.Customers;

public class CustomerStatusChangeResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }
}
