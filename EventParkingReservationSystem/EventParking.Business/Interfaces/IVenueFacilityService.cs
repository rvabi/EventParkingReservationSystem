using EventParking.Models.Entities;

namespace EventParking.Business.Interfaces;

public interface IVenueFacilityService
{
    Task<VenueFacility?> GetByIdAsync(int facilityId);

    Task<IReadOnlyList<VenueFacility>> GetByVenueIdAsync(int venueId);

    Task<bool> CreateAsync(VenueFacility facility);

    Task<bool> UpdateAsync(VenueFacility facility);

    Task<bool> DeleteAsync(int facilityId);
}