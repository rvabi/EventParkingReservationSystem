using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class FoodStallRepository : IFoodStallRepository
{
    private readonly ApplicationDbContext _context;

    public FoodStallRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FoodStall?> GetByIdAsync(int foodStallId)
    {
        return await _context.FoodStalls
            .FirstOrDefaultAsync(
                foodStall => foodStall.Id == foodStallId);
    }

    public async Task<IReadOnlyList<FoodStall>> GetByEventIdAsync(
        int eventId)
    {
        return await _context.FoodStalls
            .AsNoTracking()
            .Where(foodStall => foodStall.EventId == eventId)
            .OrderBy(foodStall => foodStall.Name)
            .ToListAsync();
    }

    public async Task AddAsync(FoodStall foodStall)
    {
        await _context.FoodStalls.AddAsync(foodStall);
    }

    public void Update(FoodStall foodStall)
    {
        _context.FoodStalls.Update(foodStall);
    }

    public void Remove(FoodStall foodStall)
    {
        _context.FoodStalls.Remove(foodStall);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}