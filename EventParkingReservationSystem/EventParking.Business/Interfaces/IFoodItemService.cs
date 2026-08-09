using EventParking.Business.DTOs.FoodCourt;

namespace EventParking.Business.Interfaces;

public interface IFoodItemService
{
    Task<IReadOnlyList<FoodItemResponse>>
        GetByFoodStallIdAsync(
            int foodStallId,
            bool availableOnly = false);

    Task<FoodItemResponse?> GetByIdAsync(
        int foodStallId,
        int foodItemId);

    Task<FoodItemResponse> CreateAsync(
        int foodStallId,
        CreateFoodItemRequest request);

    Task<FoodItemResponse?> UpdateAsync(
        int foodStallId,
        int foodItemId,
        UpdateFoodItemRequest request);
}