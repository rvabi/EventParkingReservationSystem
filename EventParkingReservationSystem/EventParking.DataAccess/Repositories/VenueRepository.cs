using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class VenueRepository : IVenueRepository
{
    private readonly ApplicationDbContext _context;

    public VenueRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Venue?> GetByIdAsync(int venueId)
    {
        return await _context.Venues
            .AsNoTracking()
            .FirstOrDefaultAsync(venue => venue.Id == venueId);
    }

    public async Task<IReadOnlyList<Venue>> GetAllAsync()
    {
        return await _context.Venues
            .AsNoTracking()
            .OrderBy(venue => venue.Name)
            .ToListAsync();
    }

    public async Task AddAsync(Venue venue)
    {
        await _context.Venues.AddAsync(venue);
    }

    public void Update(Venue venue)
    {
        _context.Venues.Update(venue);
    }

    public void Delete(Venue venue)
    {
        _context.Venues.Remove(venue);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}