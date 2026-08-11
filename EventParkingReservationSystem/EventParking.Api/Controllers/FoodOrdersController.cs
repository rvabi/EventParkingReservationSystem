using System.Security.Claims;
using EventParking.Business.DTOs.FoodCourt;
using EventParking.Business.Interfaces;
using EventParking.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/food-orders")]
[Authorize]
public class FoodOrdersController : ControllerBase
{
    private readonly IFoodOrderService _foodOrderService;

    public FoodOrdersController(
        IFoodOrderService foodOrderService)
    {
        _foodOrderService = foodOrderService;
    }

    // POST: /api/food-orders
    // Customer only
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<ActionResult<FoodOrderResponse>> Create(
        [FromBody] CreateFoodOrderRequest request)
    {
        int? customerId = GetAuthenticatedCustomerId();

        if (!customerId.HasValue)
        {
            return Unauthorized(new
            {
                message = "Invalid authentication token."
            });
        }

        try
        {
            var foodOrder =
                await _foodOrderService.CreateAsync(
                    customerId.Value,
                    request);

            return CreatedAtAction(
                nameof(GetMyOrders),
                routeValues: null,
                value: foodOrder);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = exception.Message
                });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    // GET: /api/food-orders/my-orders
    // Customer only
    [HttpGet("my-orders")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<
        ActionResult<IReadOnlyList<FoodOrderResponse>>>
        GetMyOrders()
    {
        int? customerId = GetAuthenticatedCustomerId();

        if (!customerId.HasValue)
        {
            return Unauthorized(new
            {
                message = "Invalid authentication token."
            });
        }

        var foodOrders =
            await _foodOrderService.GetMyOrdersAsync(
                customerId.Value);

        return Ok(foodOrders);
    }

    // GET: /api/food-orders
    // GET: /api/food-orders?status=Preparing
    // Administrator only
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<
        ActionResult<IReadOnlyList<FoodOrderResponse>>>
        GetAll(
            [FromQuery] FoodOrderStatus? status = null)
    {
        var foodOrders =
            await _foodOrderService.GetAllAsync(status);

        return Ok(foodOrders);
    }

    // PUT: /api/food-orders/{foodOrderId}/status
    // Administrator only
    [HttpPut("{foodOrderId:int}/status")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<FoodOrderResponse>>
        UpdateStatus(
            int foodOrderId,
            [FromBody] UpdateFoodOrderStatusRequest request)
    {
        if (foodOrderId <= 0)
        {
            return BadRequest(new
            {
                message =
                    "A valid food order ID is required."
            });
        }

        try
        {
            var foodOrder =
                await _foodOrderService.UpdateStatusAsync(
                    foodOrderId,
                    request.Status);

            if (foodOrder is null)
            {
                return NotFound(new
                {
                    message = "Food order not found."
                });
            }

            return Ok(foodOrder);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    private int? GetAuthenticatedCustomerId()
    {
        string? customerIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                customerIdValue,
                out int customerId))
        {
            return null;
        }

        return customerId > 0
            ? customerId
            : null;
    }
}