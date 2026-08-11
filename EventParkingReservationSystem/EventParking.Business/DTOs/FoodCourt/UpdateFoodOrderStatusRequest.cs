using System.ComponentModel.DataAnnotations;
using EventParking.Models.Enums;

namespace EventParking.Business.DTOs.FoodCourt;

public class UpdateFoodOrderStatusRequest
{
    [EnumDataType(
        typeof(FoodOrderStatus),
        ErrorMessage = "Invalid food order status.")]
    public FoodOrderStatus Status { get; set; }
}
