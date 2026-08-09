using System.Collections.Generic;
using System.Threading.Tasks;
using EventParking.Api.Controllers;
using EventParking.Business.DTOs.Seats;
using EventParking.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EventParking.Tests.UnitTests.Seats;

public class SeatsControllerTests
{
    private readonly Mock<ISeatService> _seatServiceMock;
    private readonly SeatsController _controller;

    public SeatsControllerTests()
    {
        _seatServiceMock = new Mock<ISeatService>();
        _controller = new SeatsController(_seatServiceMock.Object);
    }

    [Fact]
    public async Task GetSeatMap_ExistingEvent_Returns200Ok()
    {
        // Arrange
        var seatMap = new SeatMapResponseDto
        {
            EventId = 1,
            Capacity = 100,
            TicketPrice = 2500m,
            TotalSeats = 10
        };

        _seatServiceMock.Setup(s => s.GetSeatMapAsync(1)).ReturnsAsync(seatMap);

        // Act
        var actionResult = await _controller.GetSeatMap(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var resultDto = Assert.IsType<SeatMapResponseDto>(okResult.Value);
        Assert.Equal(1, resultDto.EventId);
        Assert.Equal(10, resultDto.TotalSeats);
    }

    [Fact]
    public async Task GetSeatMap_NonexistentEvent_Returns404NotFound()
    {
        // Arrange
        _seatServiceMock.Setup(s => s.GetSeatMapAsync(122)).ReturnsAsync((SeatMapResponseDto?)null);

        // Act
        var actionResult = await _controller.GetSeatMap(122);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task ValidateSeatSelection_ValidSelection_Returns200Ok()
    {
        // Arrange
        _seatServiceMock.Setup(s => s.ValidateSeatSelectionAsync(1, It.IsAny<IEnumerable<int>>())).ReturnsAsync(true);

        // Act
        var actionResult = await _controller.ValidateSeatSelection(1, new List<int> { 1, 2 });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ValidateSeatSelection_InvalidSelection_Returns400BadRequest()
    {
        // Arrange
        _seatServiceMock.Setup(s => s.ValidateSeatSelectionAsync(1, It.IsAny<IEnumerable<int>>())).ReturnsAsync(false);

        // Act
        var actionResult = await _controller.ValidateSeatSelection(1, new List<int> { 1, 1 });

        // Assert
        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public async Task UpdateSeat_NonexistentSeat_Returns404NotFound()
    {
        // Arrange
        _seatServiceMock.Setup(s => s.GetByIdAsync(99999)).ReturnsAsync((SeatDto?)null);

        var dto = new SeatDto { SeatNumber = "Z99", RowLabel = "Z", ColumnNumber = 99 };

        // Act
        var actionResult = await _controller.UpdateSeat(1, 99999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public async Task DeleteSeat_NonexistentSeat_Returns404NotFound()
    {
        // Arrange
        _seatServiceMock.Setup(s => s.GetByIdAsync(99999)).ReturnsAsync((SeatDto?)null);

        // Act
        var actionResult = await _controller.DeleteSeat(1, 99999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }
}
