using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class EventCategoryRepository : IEventCategoryRepository
{
    private readonly ApplicationDbContext _context;

    public EventCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EventCategory?> GetByIdAsync(int categoryId)
    {
        return await _context.EventCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == categoryId);
    }

    public async Task<IReadOnlyList<EventCategory>> GetAllAsync()
    {
        return await _context.EventCategories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync();
    }

    public async Task AddAsync(EventCategory category)
    {
        await _context.EventCategories.AddAsync(category);
    }

    public void Update(EventCategory category)
    {
        _context.EventCategories.Update(category);
    }

    public void Delete(EventCategory category)
    {
        _context.EventCategories.Remove(category);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}