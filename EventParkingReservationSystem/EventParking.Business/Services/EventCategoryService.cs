using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;

namespace EventParking.Business.Services;

public class EventCategoryService : IEventCategoryService
{
    private readonly IEventCategoryRepository _categoryRepository;
    private readonly IEventRepository _eventRepository;

    public EventCategoryService(
        IEventCategoryRepository categoryRepository,
        IEventRepository eventRepository)
    {
        _categoryRepository = categoryRepository;
        _eventRepository = eventRepository;
    }

    public async Task<EventCategory?> GetByIdAsync(int categoryId)
    {
        return await _categoryRepository.GetByIdAsync(categoryId);
    }

    public async Task<IReadOnlyList<EventCategory>> GetAllAsync()
    {
        return await _categoryRepository.GetAllAsync();
    }

    public async Task<bool> CreateAsync(EventCategory category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            return false;
        }

        category.Name = category.Name.Trim();

        if (!string.IsNullOrWhiteSpace(category.Description))
        {
            category.Description = category.Description.Trim();
        }

        await _categoryRepository.AddAsync(category);

        return await _categoryRepository.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(EventCategory category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            return false;
        }

        var existingCategory =
            await _categoryRepository.GetByIdAsync(category.Id);

        if (existingCategory is null)
        {
            return false;
        }

        existingCategory.Name = category.Name.Trim();

        existingCategory.Description =
            string.IsNullOrWhiteSpace(category.Description)
                ? null
                : category.Description.Trim();

        existingCategory.UpdatedAt = DateTime.UtcNow;

        _categoryRepository.Update(existingCategory);

        return await _categoryRepository.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int categoryId)
    {
        var category =
            await _categoryRepository.GetByIdAsync(categoryId);

        if (category is null)
        {
            return false;
        }

        var events = await _eventRepository.GetAllAsync();

        var categoryInUse = events.Any(eventEntity =>
            eventEntity.EventCategoryId == categoryId);

        if (categoryInUse)
        {
            return false;
        }

        _categoryRepository.Delete(category);

        return await _categoryRepository.SaveChangesAsync() > 0;
    }
}