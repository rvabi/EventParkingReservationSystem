using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventParking.Business.DTOs.Seats;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;
using Xunit;

namespace EventParking.Tests.UnitTests.Seats;

public class SeatServiceTests
{
    private readonly Mock<ISeatRepository> _seatRepositoryMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly SeatService _seatService;

    public SeatServiceTests()
    {
        _seatRepositoryMock = new Mock<ISeatRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _seatService = new SeatService(_seatRepositoryMock.Object, _eventRepositoryMock.Object);
    }

    [Fact]
    public async Task GetSeatMapAsync_ValidEvent_ReturnsMapAndEffectivePricing()
    {
        // Arrange
        var eventEntity = new Event
        {
            Id = 1,
            Capacity = 10,
            TicketPrice = 2000m
        };

        var seats = new List<Seat>
        {
            new Seat { Id = 1, EventId = 1, RowLabel = "A", ColumnNumber = 1, SeatNumber = "A1", Status = SeatStatus.Available, PriceOverride = 3000m },
            new Seat { Id = 2, EventId = 1, RowLabel = "A", ColumnNumber = 2, SeatNumber = "A2", Status = SeatStatus.Available, PriceOverride = null }
        };

        _eventRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(eventEntity);
        _seatRepositoryMock.Setup(r => r.GetByEventIdAsync(1)).ReturnsAsync(seats);

        // Act
        var result = await _seatService.GetSeatMapAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.EventId);
        Assert.Equal(10, result.Capacity);
        Assert.Equal(2000m, result.TicketPrice);
        Assert.Equal(2, result.TotalSeats);
        Assert.Equal(3000m, result.Seats[0].Price); // Override price
        Assert.Equal(2000m, result.Seats[1].Price); // Fallback price
    }

    [Fact]
    public async Task GenerateSeatMapAsync_CapacityMismatch_FailsValidation()
    {
        // Arrange
        var eventEntity = new Event
        {
            Id = 1,
            Capacity = 10,
            TicketPrice = 1500m
        };

        var request = new GenerateSeatMapRequest
        {
            Rows = new List<RowDefinitionRequest>
            {
                new RowDefinitionRequest { SeatCount = 5, RowPrice = 1500m } // Total 5 != Capacity 10
            }
        };

        _eventRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(eventEntity);
        _seatRepositoryMock.Setup(r => r.HasSeatsAsync(1)).ReturnsAsync(false);

        // Act
        var result = await _seatService.GenerateSeatMapAsync(1, request);

        // Assert
        Assert.Null(result);
        _seatRepositoryMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Seat>>()), Times.Never);
    }

    [Fact]
    public async Task GenerateSeatMapAsync_ExactCapacity_GeneratesRowsAndSeats()
    {
        // Arrange
        var eventEntity = new Event
        {
            Id = 1,
            Capacity = 3,
            TicketPrice = 1000m
        };

        var request = new GenerateSeatMapRequest
        {
            Rows = new List<RowDefinitionRequest>
            {
                new RowDefinitionRequest { SeatCount = 2, RowPrice = 1200m }, // Row A: A1, A2
                new RowDefinitionRequest { SeatCount = 1, RowPrice = null }  // Row B: B1
            }
        };

        _eventRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(eventEntity);
        _seatRepositoryMock.Setup(r => r.HasSeatsAsync(1)).ReturnsAsync(false);
        _seatRepositoryMock.Setup(r => r.GetByEventIdAsync(1)).ReturnsAsync(new List<Seat>());

        List<Seat> createdSeats = null!;
        _seatRepositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Seat>>()))
            .Callback<IEnumerable<Seat>>(s => createdSeats = s.ToList())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _seatService.GenerateSeatMapAsync(1, request);

        // Assert
        Assert.NotNull(createdSeats);
        Assert.Equal(3, createdSeats.Count);
        Assert.Equal("A", createdSeats[0].RowLabel);
        Assert.Equal("A1", createdSeats[0].SeatNumber);
        Assert.Equal("A2", createdSeats[1].SeatNumber);
        Assert.Equal("B", createdSeats[2].RowLabel);
        Assert.Equal("B1", createdSeats[2].SeatNumber);
        Assert.All(createdSeats, s => Assert.Equal(SeatStatus.Available, s.Status));
        Assert.Equal(1200m, createdSeats[0].PriceOverride);
        Assert.Null(createdSeats[2].PriceOverride);
    }

    [Fact]
    public async Task UpdateRowPriceAsync_HeldOrBookedSeatInRow_BlocksUpdate()
    {
        // Arrange
        var request = new UpdateRowPriceRequest { RowPrice = 2500m };
        var eventEntity = new Event { Id = 1, TicketPrice = 1500m };

        _eventRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(eventEntity);
        _seatRepositoryMock.Setup(r => r.RowHasHeldOrBookedSeatsAsync(1, "A")).ReturnsAsync(true);

        // Act
        var result = await _seatService.UpdateRowPriceAsync(1, "A", request);

        // Assert
        Assert.False(result);
        _seatRepositoryMock.Verify(r => r.UpdateRange(It.IsAny<IEnumerable<Seat>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRowPriceAsync_AvailableRow_AllowsPriceUpdate()
    {
        // Arrange
        var request = new UpdateRowPriceRequest { RowPrice = 2500m };
        var eventEntity = new Event { Id = 1, TicketPrice = 1500m };
        var seats = new List<Seat>
        {
            new Seat { Id = 1, EventId = 1, RowLabel = "A", ColumnNumber = 1, Status = SeatStatus.Available }
        };

        _eventRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(eventEntity);
        _seatRepositoryMock.Setup(r => r.RowHasHeldOrBookedSeatsAsync(1, "A")).ReturnsAsync(false);
        _seatRepositoryMock.Setup(r => r.GetByEventIdAndRowAsync(1, "A")).ReturnsAsync(seats);

        // Act
        var result = await _seatService.UpdateRowPriceAsync(1, "A", request);

        // Assert
        Assert.True(result);
        Assert.Equal(2500m, seats[0].PriceOverride);
        _seatRepositoryMock.Verify(r => r.UpdateRange(seats), Times.Once);
    }

    [Fact]
    public async Task ValidateSeatSelectionAsync_ValidAvailableSeats_Succeeds()
    {
        // Arrange
        var eventEntity = new Event { Id = 1 };
        var seats = new List<Seat>
        {
            new Seat { Id = 1, EventId = 1, Status = SeatStatus.Available },
            new Seat { Id = 2, EventId = 1, Status = SeatStatus.Available }
        };

        _eventRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(eventEntity);
        _seatRepositoryMock.Setup(r => r.GetByEventIdAndIdsAsync(1, It.IsAny<IEnumerable<int>>())).ReturnsAsync(seats);

        // Act
        var result = await _seatService.ValidateSeatSelectionAsync(1, new[] { 1, 2 });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateSeatSelectionAsync_EmptySelection_Fails()
    {
        // Act
        var result = await _seatService.ValidateSeatSelectionAsync(1, Array.Empty<int>());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSeatSelectionAsync_DuplicateSeatIds_Fails()
    {
        // Act
        var result = await _seatService.ValidateSeatSelectionAsync(1, new[] { 1, 1 });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSeatSelectionAsync_NonexistentSeatId_Fails()
    {
        // Arrange
        var eventEntity = new Event { Id = 1 };
        _eventRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(eventEntity);
        _seatRepositoryMock.Setup(r => r.GetByEventIdAndIdsAsync(1, It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Seat>());

        // Act
        var result = await _seatService.ValidateSeatSelectionAsync(1, new[] { 999 });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSeatSelectionAsync_NonAvailableSeat_Fails()
    {
        // Arrange
        var eventEntity = new Event { Id = 1 };
        var seats = new List<Seat>
        {
            new Seat { Id = 1, EventId = 1, Status = SeatStatus.Booked }
        };

        _eventRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(eventEntity);
        _seatRepositoryMock.Setup(r => r.GetByEventIdAndIdsAsync(1, It.IsAny<IEnumerable<int>>())).ReturnsAsync(seats);

        // Act
        var result = await _seatService.ValidateSeatSelectionAsync(1, new[] { 1 });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateSeatAsync_RenumberHeldOrBookedSeat_BlocksUpdate()
    {
        // Arrange
        var existingSeat = new Seat { Id = 1, EventId = 1, RowLabel = "A", ColumnNumber = 1, SeatNumber = "A1", Status = SeatStatus.Booked };
        var dto = new SeatDto { SeatNumber = "B1", RowLabel = "B", ColumnNumber = 1, Status = SeatStatus.Booked };

        _seatRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingSeat);

        // Act
        var result = await _seatService.UpdateSeatAsync(1, 1, dto);

        // Assert
        Assert.False(result);
        _seatRepositoryMock.Verify(r => r.Update(It.IsAny<Seat>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSeatAsync_HeldOrBookedSeat_BlocksDelete()
    {
        // Arrange
        var existingSeat = new Seat { Id = 1, EventId = 1, Status = SeatStatus.Held };
        _seatRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingSeat);

        // Act
        var result = await _seatService.DeleteSeatAsync(1, 1);

        // Assert
        Assert.False(result);
        _seatRepositoryMock.Verify(r => r.Remove(It.IsAny<Seat>()), Times.Never);
    }
}
