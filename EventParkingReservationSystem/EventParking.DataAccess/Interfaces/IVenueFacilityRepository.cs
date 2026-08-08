using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IVenueFacilityRepository
{
    Task<VenueFacility?> GetByIdAsync(int facilityId);

    Task<IReadOnlyList<VenueFacility>> GetByVenueIdAsync(int venueId);

    Task AddAsync(VenueFacility facility);

    void Update(VenueFacility facility);

    void Delete(VenueFacility facility);

    Task<int> SaveChangesAsync();
}