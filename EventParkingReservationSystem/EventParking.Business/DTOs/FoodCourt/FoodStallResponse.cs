namespace EventParking.Business.DTOs.FoodCourt;

public class FoodStallResponse
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}