using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IEventCategoryRepository
{
    Task<EventCategory?> GetByIdAsync(int categoryId);

    Task<IReadOnlyList<EventCategory>> GetAllAsync();

    Task AddAsync(EventCategory category);

    void Update(EventCategory category);

    void Delete(EventCategory category);

    Task<int> SaveChangesAsync();
}