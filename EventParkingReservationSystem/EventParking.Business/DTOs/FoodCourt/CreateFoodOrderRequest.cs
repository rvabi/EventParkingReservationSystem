using System.ComponentModel.DataAnnotations;

namespace EventParking.Business.DTOs.FoodCourt;

public class CreateFoodOrderRequest
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "A valid booking ID is required.")]
    public int BookingId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "A valid food stall ID is required.")]
    public int FoodStallId { get; set; }

    public DateTime PickupTime { get; set; }

    [Required(ErrorMessage = "At least one food item is required.")]
    [MinLength(
        1,
        ErrorMessage = "At least one food item is required.")]
    public List<CreateFoodOrderItemRequest> Items { get; set; }
        = new();
}