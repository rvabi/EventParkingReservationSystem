namespace EventParking.Business.DTOs.FoodCourt;

public class FoodOrderResponse
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public int CustomerId { get; set; }

    public int FoodStallId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime PickupTime { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public IReadOnlyList<FoodOrderItemResponse> Items { get; set; }
        = Array.Empty<FoodOrderItemResponse>();

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
