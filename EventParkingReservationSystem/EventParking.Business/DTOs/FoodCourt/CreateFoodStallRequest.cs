using System.ComponentModel.DataAnnotations;

namespace EventParking.Business.DTOs.FoodCourt;

public class CreateFoodStallRequest
{
    [Required(ErrorMessage = "Food stall name is required.")]
    [StringLength(
        100,
        ErrorMessage = "Food stall name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(
        500,
        ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Food stall status is required.")]
    [StringLength(
        30,
        ErrorMessage = "Food stall status cannot exceed 30 characters.")]
    public string Status { get; set; } = string.Empty;
}
