using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;

namespace EventParking.Business.Services;

public class VenueFacilityService : IVenueFacilityService
{
    private readonly IVenueFacilityRepository _facilityRepository;
    private readonly IVenueRepository _venueRepository;

    public VenueFacilityService(
        IVenueFacilityRepository facilityRepository,
        IVenueRepository venueRepository)
    {
        _facilityRepository = facilityRepository;
        _venueRepository = venueRepository;
    }

    public async Task<VenueFacility?> GetByIdAsync(int facilityId)
    {
        return await _facilityRepository.GetByIdAsync(facilityId);
    }

    public async Task<IReadOnlyList<VenueFacility>> GetByVenueIdAsync(int venueId)
    {
        return await _facilityRepository.GetByVenueIdAsync(venueId);
    }

    public async Task<bool> CreateAsync(VenueFacility facility)
    {
        if (!IsValidFacility(facility))
        {
            return false;
        }

        var venue =
            await _venueRepository.GetByIdAsync(facility.VenueId);

        if (venue is null)
        {
            return false;
        }

        facility.Name = facility.Name.Trim();
        facility.Zone = CleanOptional(facility.Zone);
        facility.Floor = CleanOptional(facility.Floor);
        facility.Description = CleanOptional(facility.Description);
        facility.Directions = CleanOptional(facility.Directions);

        await _facilityRepository.AddAsync(facility);

        return await _facilityRepository.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(VenueFacility facility)
    {
        if (!IsValidFacility(facility))
        {
            return false;
        }

        var existingFacility =
            await _facilityRepository.GetByIdAsync(facility.Id);

        if (existingFacility is null)
        {
            return false;
        }

        var venue =
            await _venueRepository.GetByIdAsync(facility.VenueId);

        if (venue is null)
        {
            return false;
        }

        existingFacility.VenueId = facility.VenueId;
        existingFacility.Name = facility.Name.Trim();
        existingFacility.FacilityType = facility.FacilityType;
        existingFacility.Zone = CleanOptional(facility.Zone);
        existingFacility.Floor = CleanOptional(facility.Floor);
        existingFacility.Description = CleanOptional(facility.Description);
        existingFacility.IsAccessible = facility.IsAccessible;
        existingFacility.Status = facility.Status;
        existingFacility.Directions = CleanOptional(facility.Directions);
        existingFacility.UpdatedAt = DateTime.UtcNow;

        _facilityRepository.Update(existingFacility);

        return await _facilityRepository.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int facilityId)
    {
        var facility =
            await _facilityRepository.GetByIdAsync(facilityId);

        if (facility is null)
        {
            return false;
        }

        _facilityRepository.Delete(facility);

        return await _facilityRepository.SaveChangesAsync() > 0;
    }

    private static bool IsValidFacility(VenueFacility facility)
    {
        if (facility.VenueId <= 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(facility.Name))
        {
            return false;
        }

        if (!Enum.IsDefined(facility.FacilityType))
        {
            return false;
        }

        if (!Enum.IsDefined(facility.Status))
        {
            return false;
        }

        return true;
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}