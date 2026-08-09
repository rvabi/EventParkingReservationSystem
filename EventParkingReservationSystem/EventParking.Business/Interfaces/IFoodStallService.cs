using EventParking.Business.DTOs.FoodCourt;

namespace EventParking.Business.Interfaces;

public interface IFoodStallService
{
    Task<IReadOnlyList<FoodStallResponse>> GetByEventIdAsync(
        int eventId);

    Task<FoodStallResponse?> GetByIdAsync(
        int eventId,
        int foodStallId);

    Task<FoodStallResponse> CreateAsync(
        int eventId,
        CreateFoodStallRequest request);

    Task<FoodStallResponse?> UpdateAsync(
        int eventId,
        int foodStallId,
        UpdateFoodStallRequest request);
}
