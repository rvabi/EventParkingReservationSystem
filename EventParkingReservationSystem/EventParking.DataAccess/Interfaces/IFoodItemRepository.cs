using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IFoodItemRepository
{
    Task<FoodItem?> GetByIdAsync(int foodItemId);

    Task<IReadOnlyList<FoodItem>> GetByFoodStallIdAsync(
        int foodStallId,
        bool availableOnly = false);

    Task AddAsync(FoodItem foodItem);

    void Update(FoodItem foodItem);

    void Remove(FoodItem foodItem);

    Task<int> SaveChangesAsync();
}