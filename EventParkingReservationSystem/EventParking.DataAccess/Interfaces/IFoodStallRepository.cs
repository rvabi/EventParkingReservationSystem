using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IFoodStallRepository
{
    Task<FoodStall?> GetByIdAsync(int foodStallId);

    Task<IReadOnlyList<FoodStall>> GetByEventIdAsync(int eventId);

    Task AddAsync(FoodStall foodStall);

    void Update(FoodStall foodStall);

    void Remove(FoodStall foodStall);

    Task<int> SaveChangesAsync();
}