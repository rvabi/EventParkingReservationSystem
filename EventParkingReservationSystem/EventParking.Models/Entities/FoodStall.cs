using EventParking.Models.Common;

namespace EventParking.Models.Entities;

public class FoodStall : BaseEntity
{
    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = string.Empty;

    public ICollection<FoodItem> FoodItems { get; set; }
        = new List<FoodItem>();

    public ICollection<FoodOrder> FoodOrders { get; set; }
        = new List<FoodOrder>();
}
