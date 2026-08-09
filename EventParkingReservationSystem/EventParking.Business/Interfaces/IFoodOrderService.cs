using EventParking.Business.DTOs.FoodCourt;
using EventParking.Models.Enums;

namespace EventParking.Business.Interfaces;

public interface IFoodOrderService
{
    Task<FoodOrderResponse> CreateAsync(
        int customerId,
        CreateFoodOrderRequest request);

    Task<IReadOnlyList<FoodOrderResponse>> GetMyOrdersAsync(
        int customerId);

    Task<IReadOnlyList<FoodOrderResponse>> GetAllAsync(
        FoodOrderStatus? status = null);

    Task<FoodOrderResponse?> UpdateStatusAsync(
        int foodOrderId,
        FoodOrderStatus newStatus);
}
