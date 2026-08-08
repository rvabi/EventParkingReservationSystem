using EventParking.Models.Entities;

namespace EventParking.Business.Interfaces;

public interface IVenueService
{
    Task<Venue?> GetByIdAsync(int venueId);

    Task<IReadOnlyList<Venue>> GetAllAsync();

    Task<IReadOnlyList<Venue>> GetAvailableVenuesAsync(
    DateTime startDateTime,
    DateTime endDateTime);

    Task<bool> CreateAsync(Venue venue);

    Task<bool> UpdateAsync(Venue venue);

    Task<bool> DeleteAsync(int venueId);
}