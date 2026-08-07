using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Common;
using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class Customer : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Customer;

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    public bool EmailVerified { get; set; } = false;

    public string? EmailVerificationTokenHash { get; set; }

    public DateTime? EmailVerificationTokenExpiresAt { get; set; }

    public string? PasswordResetTokenHash { get; set; }

    public DateTime? PasswordResetTokenExpiresAt { get; set; }
    public ICollection<Booking> Bookings { get; set; }
    = new List<Booking>();
    public ICollection<Payment> Payments { get; set; }
    = new List<Payment>();

    public ICollection<Notification> Notifications { get; set; }
        = new List<Notification>();
}
