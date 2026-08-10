using EventParking.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using EventParking.Business.DTOs.FoodCourt;
using EventParking.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/food-stalls")]
public class FoodStallsController : ControllerBase
{
    private readonly IFoodStallService _foodStallService;

    public FoodStallsController(
        IFoodStallService foodStallService)
    {
        _foodStallService = foodStallService;
    }

    // GET: /api/events/{eventId}/food-stalls
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FoodStallResponse>>>
        GetByEventId(int eventId)
    {
        if (eventId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid event ID is required."
            });
        }

        var foodStalls =
            await _foodStallService.GetByEventIdAsync(eventId);

        return Ok(foodStalls);
    }

    // GET: /api/events/{eventId}/food-stalls/{foodStallId}
    [HttpGet("{foodStallId:int}")]
    public async Task<ActionResult<FoodStallResponse>> GetById(
        int eventId,
        int foodStallId)
    {
        if (eventId <= 0 || foodStallId <= 0)
        {
            return BadRequest(new
            {
                message =
                    "A valid event ID and food stall ID are required."
            });
        }

        var foodStall =
            await _foodStallService.GetByIdAsync(
                eventId,
                foodStallId);

        if (foodStall is null)
        {
            return NotFound(new
            {
                message = "Food stall not found."
            });
        }

        return Ok(foodStall);
    }

    // POST: /api/events/{eventId}/food-stalls
    [HttpPost]
    public async Task<ActionResult<FoodStallResponse>> Create(
        int eventId,
        [FromBody] CreateFoodStallRequest request)
    {
        if (eventId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid event ID is required."
            });
        }

        try
        {
            var createdFoodStall =
                await _foodStallService.CreateAsync(
                    eventId,
                    request);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    eventId,
                    foodStallId = createdFoodStall.Id
                },
                createdFoodStall);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    // PUT: /api/events/{eventId}/food-stalls/{foodStallId}
    [HttpPut("{foodStallId:int}")]
    public async Task<ActionResult<FoodStallResponse>> Update(
        int eventId,
        int foodStallId,
        [FromBody] UpdateFoodStallRequest request)
    {
        if (eventId <= 0 || foodStallId <= 0)
        {
            return BadRequest(new
            {
                message =
                    "A valid event ID and food stall ID are required."
            });
        }

        try
        {
            var updatedFoodStall =
                await _foodStallService.UpdateAsync(
                    eventId,
                    foodStallId,
                    request);

            if (updatedFoodStall is null)
            {
                return NotFound(new
                {
                    message = "Food stall not found."
                });
            }

            return Ok(updatedFoodStall);
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
