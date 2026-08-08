using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IVenueRepository
{
    Task<Venue?> GetByIdAsync(int venueId);

    Task<IReadOnlyList<Venue>> GetAllAsync();

    Task AddAsync(Venue venue);

    void Update(Venue venue);

    void Delete(Venue venue);

    Task<int> SaveChangesAsync();
}