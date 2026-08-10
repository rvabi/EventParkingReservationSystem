using EventParking.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using EventParking.Business.DTOs.FoodCourt;
using EventParking.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/food-stalls/{foodStallId:int}/items")]
public class FoodItemsController : ControllerBase
{
    private readonly IFoodItemService _foodItemService;

    public FoodItemsController(
        IFoodItemService foodItemService)
    {
        _foodItemService = foodItemService;
    }

    // GET:
    // /api/food-stalls/{foodStallId}/items
    // /api/food-stalls/{foodStallId}/items?availableOnly=true
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FoodItemResponse>>>
        GetByFoodStallId(
            int foodStallId,
            [FromQuery] bool availableOnly = false)
    {
        if (foodStallId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid food stall ID is required."
            });
        }

        var foodItems =
            await _foodItemService.GetByFoodStallIdAsync(
                foodStallId,
                availableOnly);

        return Ok(foodItems);
    }

    // GET:
    // /api/food-stalls/{foodStallId}/items/{foodItemId}
    [HttpGet("{foodItemId:int}")]
    public async Task<ActionResult<FoodItemResponse>> GetById(
        int foodStallId,
        int foodItemId)
    {
        if (foodStallId <= 0 || foodItemId <= 0)
        {
            return BadRequest(new
            {
                message =
                    "A valid food stall ID and food item ID are required."
            });
        }

        var foodItem =
            await _foodItemService.GetByIdAsync(
                foodStallId,
                foodItemId);

        if (foodItem is null)
        {
            return NotFound(new
            {
                message = "Food item not found."
            });
        }

        return Ok(foodItem);
    }

    // POST:
    // /api/food-stalls/{foodStallId}/items
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<FoodItemResponse>> Create(
        int foodStallId,
        [FromBody] CreateFoodItemRequest request)
    {
        if (foodStallId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid food stall ID is required."
            });
        }

        try
        {
            var createdFoodItem =
                await _foodItemService.CreateAsync(
                    foodStallId,
                    request);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    foodStallId,
                    foodItemId = createdFoodItem.Id
                },
                createdFoodItem);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
    }

    // PUT:
    // /api/food-stalls/{foodStallId}/items/{foodItemId}
    [HttpPut("{foodItemId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<FoodItemResponse>> Update(
        int foodStallId,
        int foodItemId,
        [FromBody] UpdateFoodItemRequest request)
    {
        if (foodStallId <= 0 || foodItemId <= 0)
        {
            return BadRequest(new
            {
                message =
                    "A valid food stall ID and food item ID are required."
            });
        }

        try
        {
            var updatedFoodItem =
                await _foodItemService.UpdateAsync(
                    foodStallId,
                    foodItemId,
                    request);

            if (updatedFoodItem is null)
            {
                return NotFound(new
                {
                    message = "Food item not found."
                });
            }

            return Ok(updatedFoodItem);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }
}