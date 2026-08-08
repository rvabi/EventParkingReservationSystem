using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;

namespace EventParking.Tests.UnitTests.Parking;

public class ParkingReservationServiceTests
{
    private readonly Mock<IParkingReservationRepository>
        _repositoryMock;

    private readonly ParkingReservationService
        _service;

    public ParkingReservationServiceTests()
    {
        _repositoryMock =
            new Mock<IParkingReservationRepository>();

        _service =
            new ParkingReservationService(
                _repositoryMock.Object);
    }

    [Fact]
    public async Task ReserveAsync_AvailableSlot_CreatesHeldReservation()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 1,
            EventId = 10,
            Status = BookingStatus.Pending
        };

        var eventItem = new Event
        {
            Id = 10,
            ParkingFee = 500m
        };

        var parkingSlot = new ParkingSlot
        {
            Id = 5,
            EventId = 10,
            Event = eventItem,
            SlotNumber = "A-01",
            Zone = "Zone A",
            FeeOverride = 650m,
            Status = ParkingSlotStatus.Available
        };

        ParkingReservation? capturedReservation = null;

        _repositoryMock
            .Setup(repository =>
                repository.GetBookingByIdAsync(1))
            .ReturnsAsync(booking);

        _repositoryMock
            .Setup(repository =>
                repository.GetByBookingIdAsync(1))
            .ReturnsAsync((ParkingReservation?)null);

        _repositoryMock
            .Setup(repository =>
                repository.GetParkingSlotByIdAsync(5))
            .ReturnsAsync(parkingSlot);

        _repositoryMock
            .Setup(repository =>
                repository.HasActiveReservationForSlotAsync(5))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<ParkingReservation>()))
            .Callback<ParkingReservation>(
                reservation =>
                    capturedReservation = reservation)
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _service.ReserveAsync(1, 5);

        // Assert
        Assert.NotNull(capturedReservation);

        Assert.Equal(
            1,
            capturedReservation!.BookingId);

        Assert.Equal(
            5,
            capturedReservation.ParkingSlotId);

        Assert.Equal(
            650m,
            capturedReservation.FeeAtReservation);

        Assert.True(
            capturedReservation.IsActive);

        Assert.Equal(
            ParkingSlotStatus.Held,
            parkingSlot.Status);

        Assert.Equal(
            650m,
            result.FeeAtReservation);

        Assert.Equal(
            "Held",
            result.SlotStatus);

        _repositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ReserveAsync_BookingAlreadyHasParking_ThrowsException()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 1,
            EventId = 10,
            Status = BookingStatus.Pending
        };

        var existingReservation =
            new ParkingReservation
            {
                Id = 8,
                BookingId = 1,
                ParkingSlotId = 4,
                IsActive = true
            };

        _repositoryMock
            .Setup(repository =>
                repository.GetBookingByIdAsync(1))
            .ReturnsAsync(booking);

        _repositoryMock
            .Setup(repository =>
                repository.GetByBookingIdAsync(1))
            .ReturnsAsync(existingReservation);

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    _service.ReserveAsync(1, 5));

        // Assert
        Assert.Contains(
            "already has a parking reservation",
            exception.Message);

        _repositoryMock.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<ParkingReservation>()),
            Times.Never);
    }

    [Fact]
    public async Task ReserveAsync_ActiveReservationExistsForSlot_ThrowsException()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 1,
            EventId = 10,
            Status = BookingStatus.Pending
        };

        var parkingSlot = new ParkingSlot
        {
            Id = 5,
            EventId = 10,
            Event = new Event
            {
                Id = 10,
                ParkingFee = 500m
            },
            SlotNumber = "A-01",
            Status = ParkingSlotStatus.Available
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetBookingByIdAsync(1))
            .ReturnsAsync(booking);

        _repositoryMock
            .Setup(repository =>
                repository.GetByBookingIdAsync(1))
            .ReturnsAsync((ParkingReservation?)null);

        _repositoryMock
            .Setup(repository =>
                repository.GetParkingSlotByIdAsync(5))
            .ReturnsAsync(parkingSlot);

        _repositoryMock
            .Setup(repository =>
                repository.HasActiveReservationForSlotAsync(5))
            .ReturnsAsync(true);

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    _service.ReserveAsync(1, 5));

        // Assert
        Assert.Contains(
            "already reserved",
            exception.Message);

        _repositoryMock.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<ParkingReservation>()),
            Times.Never);
    }

    [Fact]
    public async Task ReserveAsync_SlotFromDifferentEvent_ThrowsException()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 1,
            EventId = 10,
            Status = BookingStatus.Pending
        };

        var parkingSlot = new ParkingSlot
        {
            Id = 5,
            EventId = 20,
            Event = new Event
            {
                Id = 20,
                ParkingFee = 500m
            },
            SlotNumber = "B-01",
            Status = ParkingSlotStatus.Available
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetBookingByIdAsync(1))
            .ReturnsAsync(booking);

        _repositoryMock
            .Setup(repository =>
                repository.GetByBookingIdAsync(1))
            .ReturnsAsync((ParkingReservation?)null);

        _repositoryMock
            .Setup(repository =>
                repository.GetParkingSlotByIdAsync(5))
            .ReturnsAsync(parkingSlot);

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    _service.ReserveAsync(1, 5));

        // Assert
        Assert.Contains(
            "does not belong to the booked event",
            exception.Message);
    }

    [Fact]
    public async Task ConfirmAsync_HeldReservation_ChangesSlotToReserved()
    {
        // Arrange
        var parkingSlot = new ParkingSlot
        {
            Id = 5,
            EventId = 10,
            SlotNumber = "A-01",
            Status = ParkingSlotStatus.Held
        };

        var reservation =
            new ParkingReservation
            {
                Id = 7,
                BookingId = 1,
                ParkingSlotId = 5,
                ParkingSlot = parkingSlot,
                FeeAtReservation = 500m,
                IsActive = true
            };

        _repositoryMock
            .Setup(repository =>
                repository.GetByBookingIdAsync(1))
            .ReturnsAsync(reservation);

        _repositoryMock
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _service.ConfirmAsync(1);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            ParkingSlotStatus.Reserved,
            parkingSlot.Status);

        Assert.Equal(
            "Reserved",
            result!.SlotStatus);

        Assert.NotNull(
            reservation.UpdatedAt);

        _repositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ReleaseAsync_ActiveReservation_ReleasesSlot()
    {
        // Arrange
        var parkingSlot = new ParkingSlot
        {
            Id = 5,
            EventId = 10,
            SlotNumber = "A-01",
            Status = ParkingSlotStatus.Reserved
        };

        var reservation =
            new ParkingReservation
            {
                Id = 7,
                BookingId = 1,
                ParkingSlotId = 5,
                ParkingSlot = parkingSlot,
                FeeAtReservation = 500m,
                IsActive = true
            };

        _repositoryMock
            .Setup(repository =>
                repository.GetByBookingIdAsync(1))
            .ReturnsAsync(reservation);

        _repositoryMock
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _service.ReleaseAsync(1);

        // Assert
        Assert.True(result);

        Assert.False(
            reservation.IsActive);

        Assert.Equal(
            ParkingSlotStatus.Available,
            parkingSlot.Status);

        Assert.NotNull(
            reservation.UpdatedAt);

        Assert.NotNull(
            parkingSlot.UpdatedAt);

        _repositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once);
    }
}