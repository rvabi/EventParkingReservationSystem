using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IEventCategoryRepository _categoryRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IBookingRepository _bookingRepository;

    public EventService(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        IEventCategoryRepository categoryRepository,
        ISeatRepository seatRepository,
        IBookingRepository bookingRepository)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _categoryRepository = categoryRepository;
        _seatRepository = seatRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<Event?> GetByIdAsync(int eventId)
    {
        return await _eventRepository.GetByIdAsync(eventId);
    }

    public async Task<IReadOnlyList<Event>> GetAllAsync()
    {
        return await _eventRepository.GetAllAsync();
    }


    public async Task<IReadOnlyList<Event>> SearchAsync(
    string? name,
    DateTime? date,
    int? venueId,
    int? categoryId)
    {
        return await _eventRepository.SearchAsync(
            name,
            date,
            venueId,
            categoryId);
    }


    public async Task<bool> CreateAsync(Event eventEntity)
    {
        if (!IsValidEvent(eventEntity))
        {
            return false;
        }

        var venue =
            await _venueRepository.GetByIdAsync(eventEntity.VenueId);

        if (venue is null)
        {
            return false;
        }

        var category =
            await _categoryRepository.GetByIdAsync(
                eventEntity.EventCategoryId);

        if (category is null)
        {
            return false;
        }

        if (eventEntity.Capacity > venue.TotalCapacity)
        {
            return false;
        }

        var events = await _eventRepository.GetAllAsync();

        var hasOverlap = events.Any(existingEvent =>
            existingEvent.VenueId == eventEntity.VenueId &&
            existingEvent.StartDateTime < eventEntity.EndDateTime &&
            eventEntity.StartDateTime < existingEvent.EndDateTime);

        if (hasOverlap)
        {
            return false;
        }

        eventEntity.Name = eventEntity.Name.Trim();

        if (!string.IsNullOrWhiteSpace(eventEntity.Description))
        {
            eventEntity.Description =
                eventEntity.Description.Trim();
        }

        await _eventRepository.AddAsync(eventEntity);

        return await _eventRepository.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Event eventEntity)
    {
        if (!IsValidEvent(eventEntity))
        {
            return false;
        }



        var existingEvent =
            await _eventRepository.GetByIdAsync(eventEntity.Id);

        if (existingEvent is null)
        {
            return false;
        }

        var eventBookings =
    await _bookingRepository.GetBookingsAsync(
        eventEntity.Id,
        null);

        var hasBookings =
            eventBookings.Count > 0;

        if (hasBookings &&
            (eventEntity.TicketPrice != existingEvent.TicketPrice ||
             eventEntity.Capacity != existingEvent.Capacity))
        {
            return false;
        }

        var venue =
            await _venueRepository.GetByIdAsync(eventEntity.VenueId);

        if (venue is null)
        {
            return false;
        }

        var category =
            await _categoryRepository.GetByIdAsync(
                eventEntity.EventCategoryId);

        if (category is null)
        {
            return false;
        }

        if (eventEntity.Capacity > venue.TotalCapacity)
        {
            return false;
        }

        var eventSeats =
    await _seatRepository.GetByEventIdAsync(
        eventEntity.Id);

        var bookedSeatCount =
            eventSeats.Count(
                seat => seat.Status == SeatStatus.Booked);

        if (eventEntity.Capacity < bookedSeatCount)
        {
            return false;
        }

        var events = await _eventRepository.GetAllAsync();

        var hasOverlap = events.Any(otherEvent =>
            otherEvent.Id != eventEntity.Id &&
            otherEvent.VenueId == eventEntity.VenueId &&
            otherEvent.StartDateTime < eventEntity.EndDateTime &&
            eventEntity.StartDateTime < otherEvent.EndDateTime);

        if (hasOverlap)
        {
            return false;
        }

        existingEvent.Name = eventEntity.Name.Trim();

        existingEvent.Description =
            string.IsNullOrWhiteSpace(eventEntity.Description)
                ? null
                : eventEntity.Description.Trim();

        existingEvent.VenueId = eventEntity.VenueId;
        existingEvent.EventCategoryId =
            eventEntity.EventCategoryId;

        existingEvent.StartDateTime =
            eventEntity.StartDateTime;

        existingEvent.EndDateTime =
            eventEntity.EndDateTime;

        existingEvent.TicketPrice =
            eventEntity.TicketPrice;

        existingEvent.ParkingFee =
            eventEntity.ParkingFee;

        existingEvent.Capacity =
            eventEntity.Capacity;

        existingEvent.UpdatedAt = DateTime.UtcNow;

        _eventRepository.Update(existingEvent);

        return await _eventRepository.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int eventId)
    {
        var eventEntity =
            await _eventRepository.GetByIdAsync(eventId);

        if (eventEntity is null)
        {
            return false;
        }
        var eventBookings =
    await _bookingRepository.GetBookingsAsync(
        eventId,
        null);

        var hasBookings = eventBookings.Count > 0;

        if (hasBookings)
        {
            return false;
        }

        _eventRepository.Delete(eventEntity);

        return await _eventRepository.SaveChangesAsync() > 0;
    }

    private static bool IsValidEvent(Event eventEntity)
    {
        if (string.IsNullOrWhiteSpace(eventEntity.Name))
        {
            return false;
        }

        if (eventEntity.VenueId <= 0 ||
            eventEntity.EventCategoryId <= 0)
        {
            return false;
        }

        if (eventEntity.EndDateTime <=
            eventEntity.StartDateTime)
        {
            return false;
        }

        if (eventEntity.TicketPrice < 0 ||
            eventEntity.ParkingFee < 0)
        {
            return false;
        }

        if (eventEntity.Capacity <= 0)
        {
            return false;
        }

        return true;
    }
}