using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;

namespace EventParking.Business.Services;

public class VenueService : IVenueService
{
    private readonly IVenueRepository _venueRepository;
    private readonly IEventRepository _eventRepository;

    public VenueService(
        IVenueRepository venueRepository,
        IEventRepository eventRepository)
    {
        _venueRepository = venueRepository;
        _eventRepository = eventRepository;
    }

    public async Task<Venue?> GetByIdAsync(int venueId)
    {
        return await _venueRepository.GetByIdAsync(venueId);
    }

    public async Task<IReadOnlyList<Venue>> GetAllAsync()
    {
        return await _venueRepository.GetAllAsync();
    }

    public async Task<IReadOnlyList<Venue>> GetAvailableVenuesAsync(
    DateTime startDateTime,
    DateTime endDateTime)
    {
        if (endDateTime <= startDateTime)
        {
            return Array.Empty<Venue>();
        }

        var venues = await _venueRepository.GetAllAsync();

        var overlappingEvents =
            await _eventRepository.GetOverlappingEventsAsync(
                startDateTime,
                endDateTime);

        var unavailableVenueIds = overlappingEvents
            .Select(eventEntity => eventEntity.VenueId)
            .ToHashSet();

        return venues
            .Where(venue => !unavailableVenueIds.Contains(venue.Id))
            .ToList();
    }

    public async Task<bool> CreateAsync(Venue venue)
    {
        if (string.IsNullOrWhiteSpace(venue.Name) ||
            string.IsNullOrWhiteSpace(venue.Address) ||
            venue.TotalCapacity <= 0)
        {
            return false;
        }

        venue.Name = venue.Name.Trim();
        venue.Address = venue.Address.Trim();

        await _venueRepository.AddAsync(venue);

        return await _venueRepository.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Venue venue)
    {
        if (string.IsNullOrWhiteSpace(venue.Name) ||
            string.IsNullOrWhiteSpace(venue.Address) ||
            venue.TotalCapacity <= 0)
        {
            return false;
        }

        var existingVenue =
            await _venueRepository.GetByIdAsync(venue.Id);

        if (existingVenue is null)
        {
            return false;
        }

        existingVenue.Name = venue.Name.Trim();
        existingVenue.Address = venue.Address.Trim();
        existingVenue.TotalCapacity = venue.TotalCapacity;
        existingVenue.UpdatedAt = DateTime.UtcNow;

        _venueRepository.Update(existingVenue);

        return await _venueRepository.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int venueId)
    {
        var venue = await _venueRepository.GetByIdAsync(venueId);

        if (venue is null)
        {
            return false;
        }

        var events = await _eventRepository.GetAllAsync();

        var hasUpcomingEvents = events.Any(eventEntity =>
            eventEntity.VenueId == venueId &&
            eventEntity.StartDateTime >= DateTime.UtcNow);

        if (hasUpcomingEvents)
        {
            return false;
        }

        _venueRepository.Delete(venue);

        return await _venueRepository.SaveChangesAsync() > 0;
    }
}