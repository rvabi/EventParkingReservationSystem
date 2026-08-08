using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;

    public EventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(int eventId)
    {
        return await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(eventEntity => eventEntity.Id == eventId);
    }



    public async Task<IReadOnlyList<Event>> GetAllAsync()
    {
        return await _context.Events
            .AsNoTracking()
            .OrderBy(eventEntity => eventEntity.StartDateTime)
            .ThenBy(eventEntity => eventEntity.Name)
            .ToListAsync();
    }





    public async Task<IReadOnlyList<Event>> GetOverlappingEventsAsync(
    DateTime startDateTime,
    DateTime endDateTime)
    {
        return await _context.Events
            .AsNoTracking()
            .Where(eventEntity =>
                eventEntity.StartDateTime < endDateTime &&
                startDateTime < eventEntity.EndDateTime)
            .ToListAsync();
    }


    public async Task<IReadOnlyList<Event>> SearchAsync(
    string? name,
    DateTime? date,
    int? venueId,
    int? categoryId)
    {
        var query = _context.Events
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(eventEntity =>
                eventEntity.Name.Contains(name));
        }

        if (date.HasValue)
        {
            query = query.Where(eventEntity =>
                eventEntity.StartDateTime.Date == date.Value.Date);
        }

        if (venueId.HasValue)
        {
            query = query.Where(eventEntity =>
                eventEntity.VenueId == venueId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(eventEntity =>
                eventEntity.EventCategoryId == categoryId.Value);
        }

        return await query
            .OrderBy(eventEntity => eventEntity.StartDateTime)
            .ThenBy(eventEntity => eventEntity.Name)
            .ToListAsync();
    }

    public async Task AddAsync(Event eventEntity)
    {
        await _context.Events.AddAsync(eventEntity);
    }

    public void Update(Event eventEntity)
    {
        _context.Events.Update(eventEntity);
    }

    public void Delete(Event eventEntity)
    {
        _context.Events.Remove(eventEntity);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}