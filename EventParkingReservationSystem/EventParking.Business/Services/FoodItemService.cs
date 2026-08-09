using EventParking.Business.DTOs.FoodCourt;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;

namespace EventParking.Business.Services;

public class FoodItemService : IFoodItemService
{
    private readonly IFoodItemRepository _foodItemRepository;
    private readonly IFoodStallRepository _foodStallRepository;

    public FoodItemService(
        IFoodItemRepository foodItemRepository,
        IFoodStallRepository foodStallRepository)
    {
        _foodItemRepository = foodItemRepository;
        _foodStallRepository = foodStallRepository;
    }

    public async Task<IReadOnlyList<FoodItemResponse>>
        GetByFoodStallIdAsync(
            int foodStallId,
            bool availableOnly = false)
    {
        if (foodStallId <= 0)
        {
            return Array.Empty<FoodItemResponse>();
        }

        var foodStall =
            await _foodStallRepository.GetByIdAsync(foodStallId);

        if (foodStall is null)
        {
            return Array.Empty<FoodItemResponse>();
        }

        var foodItems =
            await _foodItemRepository.GetByFoodStallIdAsync(
                foodStallId,
                availableOnly);

        return foodItems
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<FoodItemResponse?> GetByIdAsync(
        int foodStallId,
        int foodItemId)
    {
        if (foodStallId <= 0 || foodItemId <= 0)
        {
            return null;
        }

        var foodItem =
            await _foodItemRepository.GetByIdAsync(foodItemId);

        if (foodItem is null ||
            foodItem.FoodStallId != foodStallId)
        {
            return null;
        }

        return MapToResponse(foodItem);
    }

    public async Task<FoodItemResponse> CreateAsync(
        int foodStallId,
        CreateFoodItemRequest request)
    {
        if (foodStallId <= 0)
        {
            throw new ArgumentException(
                "A valid food stall ID is required.",
                nameof(foodStallId));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var foodStall =
            await _foodStallRepository.GetByIdAsync(foodStallId);

        if (foodStall is null)
        {
            throw new InvalidOperationException(
                "Food stall not found.");
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Food item name is required.",
                nameof(request));
        }

        if (request.Price < 0)
        {
            throw new ArgumentException(
                "Food item price cannot be negative.",
                nameof(request));
        }

        var foodItem = new FoodItem
        {
            FoodStallId = foodStallId,
            Name = name,
            Description =
                string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
            Price = request.Price,
            IsAvailable = request.IsAvailable,
            CreatedAt = DateTime.UtcNow
        };

        await _foodItemRepository.AddAsync(foodItem);

        await _foodItemRepository.SaveChangesAsync();

        return MapToResponse(foodItem);
    }

    public async Task<FoodItemResponse?> UpdateAsync(
        int foodStallId,
        int foodItemId,
        UpdateFoodItemRequest request)
    {
        if (foodStallId <= 0 || foodItemId <= 0)
        {
            return null;
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var foodItem =
            await _foodItemRepository.GetByIdAsync(foodItemId);

        if (foodItem is null ||
            foodItem.FoodStallId != foodStallId)
        {
            return null;
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Food item name is required.",
                nameof(request));
        }

        if (request.Price < 0)
        {
            throw new ArgumentException(
                "Food item price cannot be negative.",
                nameof(request));
        }

        foodItem.Name = name;

        foodItem.Description =
            string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

        foodItem.Price = request.Price;

        foodItem.IsAvailable = request.IsAvailable;

        foodItem.UpdatedAt = DateTime.UtcNow;

        _foodItemRepository.Update(foodItem);

        await _foodItemRepository.SaveChangesAsync();

        return MapToResponse(foodItem);
    }

    private static FoodItemResponse MapToResponse(
        FoodItem foodItem)
    {
        return new FoodItemResponse
        {
            Id = foodItem.Id,
            FoodStallId = foodItem.FoodStallId,
            Name = foodItem.Name,
            Description = foodItem.Description,
            Price = foodItem.Price,
            IsAvailable = foodItem.IsAvailable,
            CreatedAt = foodItem.CreatedAt,
            UpdatedAt = foodItem.UpdatedAt
        };
    }
}
