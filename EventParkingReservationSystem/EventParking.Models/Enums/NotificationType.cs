using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventParking.Models.Enums;

public enum NotificationType
{
    General = 1,
    BookingCreated = 2,
    BookingConfirmed = 3,
    BookingCancelled = 4,
    BookingExpired = 5,
    PaymentCompleted = 6,
    EventUpdated = 7,
    EventReminder = 8
}
