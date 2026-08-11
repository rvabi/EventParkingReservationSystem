using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class FoodItemRepository : IFoodItemRepository
{
    private readonly ApplicationDbContext _context;

    public FoodItemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FoodItem?> GetByIdAsync(int foodItemId)
    {
        return await _context.FoodItems
            .FirstOrDefaultAsync(
                foodItem => foodItem.Id == foodItemId);
    }

    public async Task<IReadOnlyList<FoodItem>>
        GetByFoodStallIdAsync(
            int foodStallId,
            bool availableOnly = false)
    {
        var query = _context.FoodItems
            .AsNoTracking()
            .Where(foodItem =>
                foodItem.FoodStallId == foodStallId);

        if (availableOnly)
        {
            query = query.Where(foodItem =>
                foodItem.IsAvailable);
        }

        return await query
            .OrderBy(foodItem => foodItem.Name)
            .ToListAsync();
    }

    public async Task AddAsync(FoodItem foodItem)
    {
        await _context.FoodItems.AddAsync(foodItem);
    }

    public void Update(FoodItem foodItem)
    {
        _context.FoodItems.Update(foodItem);
    }

    public void Remove(FoodItem foodItem)
    {
        _context.FoodItems.Remove(foodItem);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
