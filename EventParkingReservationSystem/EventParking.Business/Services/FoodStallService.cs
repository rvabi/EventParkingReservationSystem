using EventParking.Business.DTOs.FoodCourt;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;

namespace EventParking.Business.Services;

public class FoodStallService : IFoodStallService
{
    private readonly IFoodStallRepository _foodStallRepository;

    public FoodStallService(
        IFoodStallRepository foodStallRepository)
    {
        _foodStallRepository = foodStallRepository;
    }

    public async Task<IReadOnlyList<FoodStallResponse>>
        GetByEventIdAsync(int eventId)
    {
        if (eventId <= 0)
        {
            return Array.Empty<FoodStallResponse>();
        }

        var foodStalls =
            await _foodStallRepository.GetByEventIdAsync(eventId);

        return foodStalls
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<FoodStallResponse?> GetByIdAsync(
        int eventId,
        int foodStallId)
    {
        if (eventId <= 0 || foodStallId <= 0)
        {
            return null;
        }

        var foodStall =
            await _foodStallRepository.GetByIdAsync(foodStallId);

        if (foodStall is null ||
            foodStall.EventId != eventId)
        {
            return null;
        }

        return MapToResponse(foodStall);
    }

    public async Task<FoodStallResponse> CreateAsync(
        int eventId,
        CreateFoodStallRequest request)
    {
        if (eventId <= 0)
        {
            throw new ArgumentException(
                "A valid event ID is required.",
                nameof(eventId));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Food stall name is required.",
                nameof(request));
        }

        var status = request.Status.Trim();

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException(
                "Food stall status is required.",
                nameof(request));
        }

        var foodStall = new FoodStall
        {
            EventId = eventId,
            Name = name,
            Description =
                string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        await _foodStallRepository.AddAsync(foodStall);

        await _foodStallRepository.SaveChangesAsync();

        return MapToResponse(foodStall);
    }

    public async Task<FoodStallResponse?> UpdateAsync(
        int eventId,
        int foodStallId,
        UpdateFoodStallRequest request)
    {
        if (eventId <= 0 || foodStallId <= 0)
        {
            return null;
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var foodStall =
            await _foodStallRepository.GetByIdAsync(foodStallId);

        if (foodStall is null ||
            foodStall.EventId != eventId)
        {
            return null;
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Food stall name is required.",
                nameof(request));
        }

        var status = request.Status.Trim();

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException(
                "Food stall status is required.",
                nameof(request));
        }

        foodStall.Name = name;

        foodStall.Description =
            string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

        foodStall.Status = status;

        foodStall.UpdatedAt = DateTime.UtcNow;

        _foodStallRepository.Update(foodStall);

        await _foodStallRepository.SaveChangesAsync();

        return MapToResponse(foodStall);
    }

    private static FoodStallResponse MapToResponse(
        FoodStall foodStall)
    {
        return new FoodStallResponse
        {
            Id = foodStall.Id,
            EventId = foodStall.EventId,
            Name = foodStall.Name,
            Description = foodStall.Description,
            Status = foodStall.Status,
            CreatedAt = foodStall.CreatedAt,
            UpdatedAt = foodStall.UpdatedAt
        };
    }
}
