namespace EventParking.Business.DTOs.FoodCourt;

public class FoodOrderItemResponse
{
    public int Id { get; set; }

    public int FoodItemId { get; set; }

    public string FoodItemName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}
