using EventParking.Business.DTOs.FoodCourt;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class FoodOrderService : IFoodOrderService
{
    private readonly IFoodOrderRepository _foodOrderRepository;
    private readonly IFoodItemRepository _foodItemRepository;
    private readonly IFoodStallRepository _foodStallRepository;
    private readonly INotificationService _notificationService;

    public FoodOrderService(
        IFoodOrderRepository foodOrderRepository,
        IFoodItemRepository foodItemRepository,
        IFoodStallRepository foodStallRepository,
        INotificationService notificationService)
    {
        _foodOrderRepository = foodOrderRepository;
        _foodItemRepository = foodItemRepository;
        _foodStallRepository = foodStallRepository;
        _notificationService = notificationService;
    }

    public async Task<FoodOrderResponse> CreateAsync(
        int customerId,
        CreateFoodOrderRequest request)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException(
                "A valid customer ID is required.",
                nameof(customerId));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.BookingId <= 0)
        {
            throw new ArgumentException(
                "A valid booking ID is required.",
                nameof(request));
        }

        if (request.FoodStallId <= 0)
        {
            throw new ArgumentException(
                "A valid food stall ID is required.",
                nameof(request));
        }

        if (request.Items is null ||
            request.Items.Count == 0)
        {
            throw new ArgumentException(
                "At least one food item is required.",
                nameof(request));
        }

        var booking =
            await _foodOrderRepository.GetBookingByIdAsync(
                request.BookingId);

        if (booking is null)
        {
            throw new InvalidOperationException(
                "Booking not found.");
        }

        if (booking.CustomerId != customerId)
        {
            throw new UnauthorizedAccessException(
                "The booking does not belong to this customer.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "Food orders can only be created for a confirmed booking.");
        }

        var foodStall =
            await _foodStallRepository.GetByIdAsync(
                request.FoodStallId);

        if (foodStall is null)
        {
            throw new InvalidOperationException(
                "Food stall not found.");
        }

        if (foodStall.EventId != booking.EventId)
        {
            throw new InvalidOperationException(
                "The selected food stall does not belong to the booked event.");
        }

        if (booking.Event is null)
        {
            throw new InvalidOperationException(
                "Booking event information could not be loaded.");
        }

        if (request.PickupTime <
                booking.Event.StartDateTime ||
            request.PickupTime >
                booking.Event.EndDateTime)
        {
            throw new InvalidOperationException(
                "Pickup time must be within the event service period.");
        }

        var createdAt = DateTime.UtcNow;

        var orderItems =
            new List<FoodOrderItem>();

        decimal totalAmount = 0m;

        foreach (var requestedItem in request.Items)
        {
            if (requestedItem.Quantity <= 0)
            {
                throw new ArgumentException(
                    "Food item quantity must be greater than zero.",
                    nameof(request));
            }

            var foodItem =
                await _foodItemRepository.GetByIdAsync(
                    requestedItem.FoodItemId);

            if (foodItem is null)
            {
                throw new InvalidOperationException(
                    $"Food item with ID {requestedItem.FoodItemId} was not found.");
            }

            if (foodItem.FoodStallId != request.FoodStallId)
            {
                throw new InvalidOperationException(
                    $"Food item '{foodItem.Name}' does not belong to the selected food stall.");
            }

            if (!foodItem.IsAvailable)
            {
                throw new InvalidOperationException(
                    $"Food item '{foodItem.Name}' is currently unavailable.");
            }

            var unitPrice = foodItem.Price;

            var lineTotal =
                unitPrice * requestedItem.Quantity;

            totalAmount += lineTotal;

            orderItems.Add(new FoodOrderItem
            {
                FoodItemId = foodItem.Id,
                Quantity = requestedItem.Quantity,
                UnitPrice = unitPrice,
                LineTotal = lineTotal,
                CreatedAt = createdAt
            });
        }

        var orderNumber =
            await GenerateUniqueOrderNumberAsync();

        var foodOrder = new FoodOrder
        {
            BookingId = booking.Id,
            CustomerId = customerId,
            FoodStallId = foodStall.Id,
            OrderNumber = orderNumber,
            PickupTime = request.PickupTime,
            TotalAmount = totalAmount,
            Status = FoodOrderStatus.Pending,
            CreatedAt = createdAt,
            Items = orderItems
        };

        await _foodOrderRepository.AddAsync(foodOrder);

        await _foodOrderRepository.SaveChangesAsync();

        var createdOrder =
            await _foodOrderRepository.GetByIdAsync(
                foodOrder.Id);

        if (createdOrder is null)
        {
            throw new InvalidOperationException(
                "Food order was created but could not be retrieved.");
        }

        return MapToResponse(createdOrder);
    }

    public async Task<IReadOnlyList<FoodOrderResponse>>
        GetMyOrdersAsync(int customerId)
    {
        if (customerId <= 0)
        {
            return Array.Empty<FoodOrderResponse>();
        }

        var foodOrders =
            await _foodOrderRepository
                .GetByCustomerIdAsync(customerId);

        return foodOrders
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<IReadOnlyList<FoodOrderResponse>>
        GetAllAsync(
            FoodOrderStatus? status = null)
    {
        var foodOrders =
            await _foodOrderRepository.GetAllAsync(status);

        return foodOrders
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<FoodOrderResponse?> UpdateStatusAsync(
        int foodOrderId,
        FoodOrderStatus newStatus)
    {
        if (foodOrderId <= 0)
        {
            return null;
        }

        var foodOrder =
            await _foodOrderRepository.GetByIdAsync(
                foodOrderId);

        if (foodOrder is null)
        {
            return null;
        }

        if (!IsValidStatusTransition(
                foodOrder.Status,
                newStatus))
        {
            throw new InvalidOperationException(
                $"Invalid food order status transition from " +
                $"{foodOrder.Status} to {newStatus}.");
        }

        foodOrder.Status = newStatus;
        foodOrder.UpdatedAt = DateTime.UtcNow;

        _foodOrderRepository.Update(foodOrder);

        await _foodOrderRepository.SaveChangesAsync();

        if (newStatus == FoodOrderStatus.ReadyForPickup)
        {
            await _notificationService.NotifyFoodReadyAsync(
                foodOrder.CustomerId,
                foodOrder.OrderNumber);
        }

        return MapToResponse(foodOrder);
    }

    private async Task<string> GenerateUniqueOrderNumberAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix =
                Guid.NewGuid()
                    .ToString("N")[..6]
                    .ToUpperInvariant();

            var orderNumber =
                $"FO-{DateTime.UtcNow:yyyyMMddHHmmss}-{suffix}";

            var exists =
                await _foodOrderRepository
                    .OrderNumberExistsAsync(orderNumber);

            if (!exists)
            {
                return orderNumber;
            }
        }

        throw new InvalidOperationException(
            "Unable to generate a unique food order number.");
    }

    private static bool IsValidStatusTransition(
        FoodOrderStatus currentStatus,
        FoodOrderStatus newStatus)
    {
        if (newStatus == FoodOrderStatus.Cancelled)
        {
            return currentStatus != FoodOrderStatus.Collected &&
                   currentStatus != FoodOrderStatus.Cancelled;
        }

        return currentStatus switch
        {
            FoodOrderStatus.Pending =>
                newStatus == FoodOrderStatus.Confirmed,

            FoodOrderStatus.Confirmed =>
                newStatus == FoodOrderStatus.Preparing,

            FoodOrderStatus.Preparing =>
                newStatus == FoodOrderStatus.ReadyForPickup,

            FoodOrderStatus.ReadyForPickup =>
                newStatus == FoodOrderStatus.Collected,

            FoodOrderStatus.Collected => false,

            FoodOrderStatus.Cancelled => false,

            _ => false
        };
    }

    private static FoodOrderResponse MapToResponse(
        FoodOrder foodOrder)
    {
        return new FoodOrderResponse
        {
            Id = foodOrder.Id,
            BookingId = foodOrder.BookingId,
            CustomerId = foodOrder.CustomerId,
            FoodStallId = foodOrder.FoodStallId,
            OrderNumber = foodOrder.OrderNumber,
            PickupTime = foodOrder.PickupTime,
            TotalAmount = foodOrder.TotalAmount,
            Status = foodOrder.Status.ToString(),

            Items = foodOrder.Items
                .Select(orderItem =>
                    new FoodOrderItemResponse
                    {
                        Id = orderItem.Id,
                        FoodItemId = orderItem.FoodItemId,
                        FoodItemName =
                            orderItem.FoodItem?.Name
                            ?? string.Empty,
                        Quantity = orderItem.Quantity,
                        UnitPrice = orderItem.UnitPrice,
                        LineTotal = orderItem.LineTotal
                    })
                .ToList(),

            CreatedAt = foodOrder.CreatedAt,
            UpdatedAt = foodOrder.UpdatedAt
        };
    }
}