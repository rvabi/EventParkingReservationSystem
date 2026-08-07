using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Common;
using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class Payment : BaseEntity
{
    public int BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }
        = PaymentStatus.Pending;

    public string? TransactionReference { get; set; }

    public DateTime? PaidAt { get; set; }
}
