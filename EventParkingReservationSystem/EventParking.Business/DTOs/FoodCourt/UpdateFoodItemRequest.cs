using System.ComponentModel.DataAnnotations;

namespace EventParking.Business.DTOs.FoodCourt;

public class UpdateFoodItemRequest
{
    [Required(ErrorMessage = "Food item name is required.")]
    [StringLength(
        100,
        ErrorMessage = "Food item name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(
        500,
        ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "9999999999999999.99",
        ErrorMessage = "Food item price cannot be negative.")]
    public decimal Price { get; set; }

    public bool IsAvailable { get; set; }
}