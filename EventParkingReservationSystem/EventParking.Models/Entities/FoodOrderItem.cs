using EventParking.Models.Common;

namespace EventParking.Models.Entities;

public class FoodOrderItem : BaseEntity
{
    public int FoodOrderId { get; set; }

    public FoodOrder FoodOrder { get; set; } = null!;

    public int FoodItemId { get; set; }

    public FoodItem FoodItem { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}
