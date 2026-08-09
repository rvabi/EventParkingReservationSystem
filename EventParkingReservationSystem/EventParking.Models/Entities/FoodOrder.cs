using EventParking.Models.Common;
using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class FoodOrder : BaseEntity
{
    public int BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public int FoodStallId { get; set; }

    public FoodStall FoodStall { get; set; } = null!;

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime PickupTime { get; set; }

    public decimal TotalAmount { get; set; }

    public FoodOrderStatus Status { get; set; }
        = FoodOrderStatus.Pending;

    public ICollection<FoodOrderItem> Items { get; set; }
        = new List<FoodOrderItem>();
}
