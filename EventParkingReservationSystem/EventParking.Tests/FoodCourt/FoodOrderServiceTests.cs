using EventParking.Business.DTOs.FoodCourt;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;
using Xunit;

namespace EventParking.Tests.UnitTests.FoodCourt;

public class FoodOrderServiceTests
{
    private readonly Mock<IFoodOrderRepository>
        _foodOrderRepositoryMock;

    private readonly Mock<IFoodItemRepository>
        _foodItemRepositoryMock;

    private readonly Mock<IFoodStallRepository>
        _foodStallRepositoryMock;

    private readonly FoodOrderService _service;

    public FoodOrderServiceTests()
    {
        _foodOrderRepositoryMock =
            new Mock<IFoodOrderRepository>();

        _foodItemRepositoryMock =
            new Mock<IFoodItemRepository>();

        _foodStallRepositoryMock =
            new Mock<IFoodStallRepository>();

        _service = new FoodOrderService(
            _foodOrderRepositoryMock.Object,
            _foodItemRepositoryMock.Object,
            _foodStallRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidOrder_CalculatesTotalsAndPriceSnapshots()
    {
        // Arrange
        var customerId = 7;

        var eventItem = new Event
        {
            Id = 10,
            StartDateTime =
                new DateTime(
                    2026, 8, 10, 17, 0, 0,
                    DateTimeKind.Utc),
            EndDateTime =
                new DateTime(
                    2026, 8, 10, 22, 0, 0,
                    DateTimeKind.Utc)
        };

        var booking = new Booking
        {
            Id = 5,
            CustomerId = customerId,
            EventId = 10,
            Event = eventItem,
            Status = BookingStatus.Confirmed
        };

        var foodStall = new FoodStall
        {
            Id = 2,
            EventId = 10,
            Name = "Burger Corner",
            Status = "Active"
        };

        var burger = new FoodItem
        {
            Id = 4,
            FoodStallId = 2,
            Name = "Chicken Burger",
            Price = 850m,
            IsAvailable = true
        };

        var drink = new FoodItem
        {
            Id = 6,
            FoodStallId = 2,
            Name = "Soft Drink",
            Price = 300m,
            IsAvailable = true
        };

        var request = new CreateFoodOrderRequest
        {
            BookingId = 5,
            FoodStallId = 2,
            PickupTime =
                new DateTime(
                    2026, 8, 10, 18, 30, 0,
                    DateTimeKind.Utc),
            Items =
            [
                new CreateFoodOrderItemRequest
                {
                    FoodItemId = 4,
                    Quantity = 2
                },
                new CreateFoodOrderItemRequest
                {
                    FoodItemId = 6,
                    Quantity = 1
                }
            ]
        };

        FoodOrder? capturedOrder = null;

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.GetBookingByIdAsync(5))
            .ReturnsAsync(booking);

        _foodStallRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(2))
            .ReturnsAsync(foodStall);

        _foodItemRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(4))
            .ReturnsAsync(burger);

        _foodItemRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(6))
            .ReturnsAsync(drink);

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.OrderNumberExistsAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(false);

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<FoodOrder>()))
            .Callback<FoodOrder>(foodOrder =>
            {
                capturedOrder = foodOrder;

                foreach (var orderItem in foodOrder.Items)
                {
                    if (orderItem.FoodItemId == burger.Id)
                    {
                        orderItem.FoodItem = burger;
                    }

                    if (orderItem.FoodItemId == drink.Id)
                    {
                        orderItem.FoodItem = drink;
                    }
                }
            })
            .Returns(Task.CompletedTask);

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(
                    It.IsAny<int>()))
            .ReturnsAsync(() => capturedOrder);

        // Act
        var result =
            await _service.CreateAsync(
                customerId,
                request);

        // Assert
        Assert.NotNull(capturedOrder);

        Assert.Equal(
            FoodOrderStatus.Pending,
            capturedOrder!.Status);

        Assert.Equal(
            2000m,
            capturedOrder.TotalAmount);

        Assert.Equal(
            2,
            capturedOrder.Items.Count);

        var burgerOrderItem =
            capturedOrder.Items.Single(
                item =>
                    item.FoodItemId == 4);

        Assert.Equal(
            2,
            burgerOrderItem.Quantity);

        Assert.Equal(
            850m,
            burgerOrderItem.UnitPrice);

        Assert.Equal(
            1700m,
            burgerOrderItem.LineTotal);

        var drinkOrderItem =
            capturedOrder.Items.Single(
                item =>
                    item.FoodItemId == 6);

        Assert.Equal(
            300m,
            drinkOrderItem.UnitPrice);

        Assert.Equal(
            300m,
            drinkOrderItem.LineTotal);

        Assert.Equal(
            2000m,
            result.TotalAmount);

        Assert.Equal(
            "Pending",
            result.Status);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.OrderNumber));

        _foodOrderRepositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MenuPriceChanges_PreservesOrderPriceSnapshot()
    {
        // Arrange
        const int customerId = 7;

        var booking = CreateConfirmedBooking(
            customerId);

        var foodStall = CreateFoodStall();

        var foodItem = new FoodItem
        {
            Id = 4,
            FoodStallId = 2,
            Name = "Chicken Burger",
            Price = 850m,
            IsAvailable = true
        };

        var request = CreateOrderRequest(
            foodItemId: 4,
            quantity: 2);

        FoodOrder? capturedOrder = null;

        SetupValidOrderDependencies(
            booking,
            foodStall,
            foodItem);

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<FoodOrder>()))
            .Callback<FoodOrder>(foodOrder =>
            {
                capturedOrder = foodOrder;

                foodOrder.Items.Single().FoodItem =
     foodItem;
            })
            .Returns(Task.CompletedTask);

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(
                    It.IsAny<int>()))
            .ReturnsAsync(() => capturedOrder);

        // Act
        await _service.CreateAsync(
            customerId,
            request);

        foodItem.Price = 1000m;

        // Assert
        Assert.NotNull(capturedOrder);

        Assert.Equal(
     850m,
     capturedOrder!.Items.Single().UnitPrice);

        Assert.Equal(
            1700m,
            capturedOrder.Items.Single().LineTotal);

        Assert.Equal(
            1700m,
            capturedOrder.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_UnavailableItem_ThrowsException()
    {
        // Arrange
        const int customerId = 7;

        var booking =
            CreateConfirmedBooking(customerId);

        var foodStall =
            CreateFoodStall();

        var unavailableItem = new FoodItem
        {
            Id = 4,
            FoodStallId = 2,
            Name = "Chicken Burger",
            Price = 850m,
            IsAvailable = false
        };

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.GetBookingByIdAsync(5))
            .ReturnsAsync(booking);

        _foodStallRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(2))
            .ReturnsAsync(foodStall);

        _foodItemRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(4))
            .ReturnsAsync(unavailableItem);

        var request =
            CreateOrderRequest(
                foodItemId: 4,
                quantity: 1);

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    _service.CreateAsync(
                        customerId,
                        request));

        // Assert
        Assert.Contains(
            "currently unavailable",
            exception.Message);

        _foodOrderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<FoodOrder>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_BookingBelongsToAnotherCustomer_ThrowsException()
    {
        // Arrange
        const int authenticatedCustomerId = 7;

        var booking =
            CreateConfirmedBooking(
                customerId: 99);

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.GetBookingByIdAsync(5))
            .ReturnsAsync(booking);

        var request =
            CreateOrderRequest(
                foodItemId: 4,
                quantity: 1);

        // Act
        var exception =
            await Assert.ThrowsAsync<
                UnauthorizedAccessException>(
                () =>
                    _service.CreateAsync(
                        authenticatedCustomerId,
                        request));

        // Assert
        Assert.Contains(
            "does not belong to this customer",
            exception.Message);

        _foodOrderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<FoodOrder>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UnconfirmedBooking_ThrowsException()
    {
        // Arrange
        const int customerId = 7;

        var booking =
            CreateConfirmedBooking(customerId);

        booking.Status =
            BookingStatus.Pending;

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.GetBookingByIdAsync(5))
            .ReturnsAsync(booking);

        var request =
            CreateOrderRequest(
                foodItemId: 4,
                quantity: 1);

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    _service.CreateAsync(
                        customerId,
                        request));

        // Assert
        Assert.Contains(
            "confirmed booking",
            exception.Message);

        _foodOrderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<FoodOrder>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PickupTimeOutsideEventPeriod_ThrowsException()
    {
        // Arrange
        const int customerId = 7;

        var booking =
            CreateConfirmedBooking(customerId);

        var foodStall =
            CreateFoodStall();

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.GetBookingByIdAsync(5))
            .ReturnsAsync(booking);

        _foodStallRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(2))
            .ReturnsAsync(foodStall);

        var request =
            CreateOrderRequest(
                foodItemId: 4,
                quantity: 1);

        request.PickupTime =
            booking.Event.EndDateTime
                .AddHours(1);

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    _service.CreateAsync(
                        customerId,
                        request));

        // Assert
        Assert.Contains(
            "within the event service period",
            exception.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ThrowsException()
    {
        // Arrange
        var foodOrder =
            CreateExistingFoodOrder(
                FoodOrderStatus.Pending);

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(20))
            .ReturnsAsync(foodOrder);

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    _service.UpdateStatusAsync(
                        20,
                        FoodOrderStatus.Collected));

        // Assert
        Assert.Contains(
            "Invalid food order status transition",
            exception.Message);

        _foodOrderRepositoryMock.Verify(
            repository =>
                repository.Update(
                    It.IsAny<FoodOrder>()),
            Times.Never);

        _foodOrderRepositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_UpdatesStatus()
    {
        // Arrange
        var foodOrder =
            CreateExistingFoodOrder(
                FoodOrderStatus.Preparing);

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(20))
            .ReturnsAsync(foodOrder);

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _service.UpdateStatusAsync(
                20,
                FoodOrderStatus.ReadyForPickup);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            FoodOrderStatus.ReadyForPickup,
            foodOrder.Status);

        Assert.Equal(
            "ReadyForPickup",
            result!.Status);

        Assert.NotNull(
            foodOrder.UpdatedAt);

        _foodOrderRepositoryMock.Verify(
            repository =>
                repository.Update(foodOrder),
            Times.Once);

        _foodOrderRepositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once);
    }

    private static Booking CreateConfirmedBooking(
        int customerId)
    {
        var eventItem = new Event
        {
            Id = 10,
            StartDateTime =
                new DateTime(
                    2026, 8, 10, 17, 0, 0,
                    DateTimeKind.Utc),
            EndDateTime =
                new DateTime(
                    2026, 8, 10, 22, 0, 0,
                    DateTimeKind.Utc)
        };

        return new Booking
        {
            Id = 5,
            CustomerId = customerId,
            EventId = 10,
            Event = eventItem,
            Status = BookingStatus.Confirmed
        };
    }

    private static FoodStall CreateFoodStall()
    {
        return new FoodStall
        {
            Id = 2,
            EventId = 10,
            Name = "Burger Corner",
            Status = "Active"
        };
    }

    private static CreateFoodOrderRequest
        CreateOrderRequest(
            int foodItemId,
            int quantity)
    {
        return new CreateFoodOrderRequest
        {
            BookingId = 5,
            FoodStallId = 2,
            PickupTime =
                new DateTime(
                    2026, 8, 10, 18, 30, 0,
                    DateTimeKind.Utc),
            Items =
            [
                new CreateFoodOrderItemRequest
                {
                    FoodItemId = foodItemId,
                    Quantity = quantity
                }
            ]
        };
    }

    private void SetupValidOrderDependencies(
        Booking booking,
        FoodStall foodStall,
        FoodItem foodItem)
    {
        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.GetBookingByIdAsync(
                    booking.Id))
            .ReturnsAsync(booking);

        _foodStallRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(
                    foodStall.Id))
            .ReturnsAsync(foodStall);

        _foodItemRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(
                    foodItem.Id))
            .ReturnsAsync(foodItem);

        _foodOrderRepositoryMock
            .Setup(repository =>
                repository.OrderNumberExistsAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(false);
    }

    private static FoodOrder CreateExistingFoodOrder(
        FoodOrderStatus status)
    {
        var foodItem = new FoodItem
        {
            Id = 4,
            FoodStallId = 2,
            Name = "Chicken Burger",
            Price = 850m,
            IsAvailable = true
        };

        return new FoodOrder
        {
            Id = 20,
            BookingId = 5,
            CustomerId = 7,
            FoodStallId = 2,
            OrderNumber =
                "FO-TEST-0001",
            PickupTime =
                new DateTime(
                    2026, 8, 10, 18, 30, 0,
                    DateTimeKind.Utc),
            TotalAmount = 850m,
            Status = status,
            Items =
            [
                new FoodOrderItem
                {
                    Id = 30,
                    FoodOrderId = 20,
                    FoodItemId = 4,
                    FoodItem = foodItem,
                    Quantity = 1,
                    UnitPrice = 850m,
                    LineTotal = 850m
                }
            ]
        };
    }
}
