using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int eventId);

    Task<IReadOnlyList<Event>> GetAllAsync();

    Task<IReadOnlyList<Event>> GetOverlappingEventsAsync(
    DateTime startDateTime,
    DateTime endDateTime);

    Task<IReadOnlyList<Event>> SearchAsync(
    string? name,
    DateTime? date,
    int? venueId,
    int? categoryId);

    Task AddAsync(Event eventEntity);

    void Update(Event eventEntity);

    void Delete(Event eventEntity);

    Task<int> SaveChangesAsync();
}