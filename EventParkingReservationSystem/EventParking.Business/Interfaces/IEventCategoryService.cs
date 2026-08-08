using EventParking.Models.Entities;

namespace EventParking.Business.Interfaces;

public interface IEventCategoryService
{
    Task<EventCategory?> GetByIdAsync(int categoryId);

    Task<IReadOnlyList<EventCategory>> GetAllAsync();

    Task<bool> CreateAsync(EventCategory category);

    Task<bool> UpdateAsync(EventCategory category);

    Task<bool> DeleteAsync(int categoryId);
}