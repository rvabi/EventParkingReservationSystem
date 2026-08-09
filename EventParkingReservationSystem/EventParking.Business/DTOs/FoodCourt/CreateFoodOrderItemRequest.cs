using System.ComponentModel.DataAnnotations;

namespace EventParking.Business.DTOs.FoodCourt;

public class CreateFoodOrderItemRequest
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "A valid food item ID is required.")]
    public int FoodItemId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Quantity must be greater than zero.")]
    public int Quantity { get; set; }
}