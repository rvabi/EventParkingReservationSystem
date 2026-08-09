using EventParking.Models.Common;

namespace EventParking.Models.Entities;

public class FoodItem : BaseEntity
{
    public int FoodStallId { get; set; }

    public FoodStall FoodStall { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;

    public ICollection<FoodOrderItem> FoodOrderItems { get; set; }
        = new List<FoodOrderItem>();
}
