namespace EventParking.Business.DTOs.FoodCourt;

public class FoodItemResponse
{
    public int Id { get; set; }

    public int FoodStallId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}