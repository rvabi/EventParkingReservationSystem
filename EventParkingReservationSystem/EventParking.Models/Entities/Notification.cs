using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Common;
using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class Notification : BaseEntity
{
    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public NotificationType Type { get; set; }
        = NotificationType.General;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }
}
