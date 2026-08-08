using EventParking.Models.Entities;

namespace EventParking.Business.Interfaces;

public interface IEventService
{
    Task<Event?> GetByIdAsync(int eventId);

    Task<IReadOnlyList<Event>> GetAllAsync();


    Task<IReadOnlyList<Event>> SearchAsync(
    string? name,
    DateTime? date,
    int? venueId,
    int? categoryId);


    Task<bool> CreateAsync(Event eventEntity);

    Task<bool> UpdateAsync(Event eventEntity);

    Task<bool> DeleteAsync(int eventId);
}