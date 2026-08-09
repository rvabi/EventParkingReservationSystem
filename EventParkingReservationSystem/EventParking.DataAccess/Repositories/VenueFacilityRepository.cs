using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class VenueFacilityRepository : IVenueFacilityRepository
{
    private readonly ApplicationDbContext _context;

    public VenueFacilityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VenueFacility?> GetByIdAsync(int facilityId)
    {
        return await _context.VenueFacilities
            .AsNoTracking()
            .FirstOrDefaultAsync(facility => facility.Id == facilityId);
    }

    public async Task<IReadOnlyList<VenueFacility>> GetByVenueIdAsync(int venueId)
    {
        return await _context.VenueFacilities
            .AsNoTracking()
            .Where(facility => facility.VenueId == venueId)
            .OrderBy(facility => facility.FacilityType)
            .ThenBy(facility => facility.Name)
            .ToListAsync();
    }

    public async Task AddAsync(VenueFacility facility)
    {
        await _context.VenueFacilities.AddAsync(facility);
    }

    public void Update(VenueFacility facility)
    {
        _context.VenueFacilities.Update(facility);
    }

    public void Delete(VenueFacility facility)
    {
        _context.VenueFacilities.Remove(facility);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}