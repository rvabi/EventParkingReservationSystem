using EventParking.Business.DTOs.Bookings;
using EventParking.Business.DTOs.Payments;
using EventParking.Business.Options;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;

namespace EventParking.Tests.UnitTests.Bookings;

public sealed class BookingServiceTests
{
    [Fact]
    public async Task CreatePendingAsync_WithSeatsOnly_CreatesHeldBooking()
    {
        var repository = CreateRepository();
        var transaction = CreateTransaction();
        Booking? savedBooking = null;

        var customer = CreateCustomer();
        var eventItem = CreateEvent();
        var seats = new List<Seat>
        {
            CreateSeat(10, "A1", 50m),
            CreateSeat(11, "A2", null)
        };

        repository
            .Setup(item => item.BeginSerializableTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        repository
            .Setup(item => item.GetCustomerAsync(
                customer.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        repository
            .Setup(item => item.GetEventAsync(
                eventItem.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);
        repository
            .Setup(item => item.GetSeatsForUpdateAsync(
                eventItem.Id,
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(seats);
        repository
            .Setup(item => item.AddBookingAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>(
                (booking, _) =>
                {
                    booking.Id = 123;
                    savedBooking = booking;
                })
            .Returns(Task.CompletedTask);

        var service = CreateService(repository.Object);

        var response = await service.CreatePendingAsync(
            customer.Id,
            new CreateBookingRequest
            {
                EventId = eventItem.Id,
                SeatIds = new[] { 10, 11 }
            });

        Assert.NotNull(savedBooking);
        Assert.Equal(
            $"BKG-{DateTime.UtcNow:yyyy}-000123",
            response.BookingNumber);
        Assert.Equal(125m, response.TotalAmount);
        Assert.Equal(BookingStatus.Pending, savedBooking!.Status);
        Assert.All(seats, seat =>
            Assert.Equal(SeatStatus.Held, seat.Status));
        Assert.Equal(
            new[] { 50m, 75m },
            savedBooking.BookingSeats
                .Select(item => item.UnitPriceAtBooking)
                .ToArray());
        transaction.Verify(
            item => item.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePendingAsync_WithNoSeats_RejectsRequest()
    {
        var service = CreateService(
            CreateRepository().Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreatePendingAsync(
                1,
                new CreateBookingRequest
                {
                    EventId = 1,
                    SeatIds = Array.Empty<int>()
                }));

        Assert.Contains(
            "at least one seat",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePendingAsync_WithOptionalParking_SnapshotsFee()
    {
        var repository = CreateRepository();
        var transaction = CreateTransaction();
        var customer = CreateCustomer();
        var eventItem = CreateEvent();
        var seat = CreateSeat(10, "A1", null);
        var parkingSlot = new ParkingSlot
        {
            Id = 30,
            EventId = eventItem.Id,
            Event = eventItem,
            SlotNumber = "P1",
            Zone = "North",
            FeeOverride = 25m,
            Status = ParkingSlotStatus.Available
        };

        repository
            .Setup(item => item.BeginSerializableTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        repository
            .Setup(item => item.GetCustomerAsync(
                customer.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        repository
            .Setup(item => item.GetEventAsync(
                eventItem.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventItem);
        repository
            .Setup(item => item.GetSeatsForUpdateAsync(
                eventItem.Id,
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { seat });
        repository
            .Setup(item => item.GetParkingSlotForUpdateAsync(
                eventItem.Id,
                parkingSlot.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parkingSlot);
        repository
            .Setup(item => item.AddBookingAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>(
                (booking, _) => booking.Id = 124)
            .Returns(Task.CompletedTask);

        var service = CreateService(repository.Object);

        var response = await service.CreatePendingAsync(
            customer.Id,
            new CreateBookingRequest
            {
                EventId = eventItem.Id,
                SeatIds = new[] { seat.Id },
                ParkingSlotId = parkingSlot.Id
            });

        Assert.NotNull(response.Parking);
        Assert.Equal(25m, response.Parking!.FeeAtReservation);
        Assert.Equal(100m, response.TotalAmount);
        Assert.Equal(ParkingSlotStatus.Held, parkingSlot.Status);
    }

    [Fact]
    public async Task SimulatePaymentAsync_WhenFailure_KeepsBookingPending()
    {
        var repository = CreateRepository();
        var transaction = CreateTransaction();
        var booking = CreatePendingBooking();

        repository
            .Setup(item => item.BeginSerializableTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        repository
            .Setup(item => item.GetTrackedBookingAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var service = CreateService(repository.Object);

        var payment = await service.SimulatePaymentAsync(
            booking.Id,
            booking.CustomerId,
            new SimulatePaymentRequest
            {
                SimulateSuccess = false
            });

        Assert.Equal(
            PaymentStatus.Failed.ToString(),
            payment.Status);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(SeatStatus.Held,
            booking.BookingSeats.Single().Seat.Status);
        Assert.Null(payment.PaidAt);
    }

    [Fact]
    public async Task SimulatePaymentAsync_AfterFailure_CanConfirmSamePayment()
    {
        var repository = CreateRepository();
        var transaction = CreateTransaction();
        var booking = CreatePendingBooking();
        var existingPayment = new Payment
        {
            Id = 91,
            BookingId = booking.Id,
            Booking = booking,
            CustomerId = booking.CustomerId,
            Customer = booking.Customer,
            Amount = booking.TotalAmount,
            Status = PaymentStatus.Failed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        booking.Payment = existingPayment;

        repository
            .Setup(item => item.BeginSerializableTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        repository
            .Setup(item => item.GetTrackedBookingAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var service = CreateService(repository.Object);

        var payment = await service.SimulatePaymentAsync(
            booking.Id,
            booking.CustomerId,
            new SimulatePaymentRequest
            {
                SimulateSuccess = true
            });

        Assert.Equal(91, payment.Id);
        Assert.Equal(
            PaymentStatus.Completed.ToString(),
            payment.Status);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Null(booking.HoldExpiresAt);
        Assert.Equal(
            SeatStatus.Booked,
            booking.BookingSeats.Single().Seat.Status);
        Assert.NotNull(payment.PaidAt);
    }

    [Fact]
    public async Task CancelPendingAsync_AfterPayment_IsRejected()
    {
        var repository = CreateRepository();
        var transaction = CreateTransaction();
        var booking = CreatePendingBooking();
        booking.Status = BookingStatus.Confirmed;

        repository
            .Setup(item => item.BeginSerializableTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        repository
            .Setup(item => item.GetTrackedBookingAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var service = CreateService(repository.Object);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CancelPendingAsync(
                    booking.Id,
                    booking.CustomerId,
                    false));

        Assert.Contains(
            "unpaid pending",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetByIdAsync_ForAnotherCustomer_IsRejected()
    {
        var repository = CreateRepository();
        var booking = CreatePendingBooking();

        repository
            .Setup(item => item.GetBookingDetailsAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var service = CreateService(repository.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetByIdAsync(
                booking.Id,
                booking.CustomerId + 1,
                false));
    }

    [Fact]
    public async Task SimulatePaymentAsync_AfterHoldExpiry_ExpiresBooking()
    {
        var repository = CreateRepository();
        var transaction = CreateTransaction();
        var booking = CreatePendingBooking();
        booking.HoldExpiresAt = DateTime.UtcNow.AddSeconds(-1);

        repository
            .Setup(item => item.BeginSerializableTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        repository
            .Setup(item => item.GetTrackedBookingAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var service = CreateService(repository.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SimulatePaymentAsync(
                booking.Id,
                booking.CustomerId,
                new SimulatePaymentRequest
                {
                    SimulateSuccess = true
                }));

        Assert.Equal(BookingStatus.Expired, booking.Status);
        Assert.Equal(
            SeatStatus.Available,
            booking.BookingSeats.Single().Seat.Status);
    }

    [Fact]
    public async Task ExpirePendingBookingsAsync_ReleasesHeldResourcesOnce()
    {
        var repository = CreateRepository();
        var transaction = CreateTransaction();
        var booking = CreatePendingBooking();
        booking.HoldExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        repository
            .Setup(item => item.GetExpiredPendingBookingIdsAsync(
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { booking.Id });
        repository
            .Setup(item => item.BeginSerializableTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        repository
            .Setup(item => item.GetTrackedBookingAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var service = CreateService(repository.Object);

        var expired =
            await service.ExpirePendingBookingsAsync();

        Assert.Equal(1, expired);
        Assert.Equal(BookingStatus.Expired, booking.Status);
        Assert.Equal(
            SeatStatus.Available,
            booking.BookingSeats.Single().Seat.Status);
    }

    private static BookingService CreateService(
        IBookingRepository repository)
    {
        return new BookingService(
            repository,
            new BookingOptions
            {
                HoldMinutes = 15,
                ExpiryScanSeconds = 60
            });
    }

    private static Mock<IBookingRepository> CreateRepository()
    {
        var repository = new Mock<IBookingRepository>();

        repository
            .Setup(item => item.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        repository
            .Setup(item => item.AddNotificationsAsync(
                It.IsAny<IEnumerable<Notification>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return repository;
    }

    private static Mock<IRepositoryTransaction> CreateTransaction()
    {
        var transaction = new Mock<IRepositoryTransaction>();
        transaction
            .Setup(item => item.CommitAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        transaction
            .Setup(item => item.RollbackAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        transaction
            .Setup(item => item.DisposeAsync())
            .Returns(ValueTask.CompletedTask);
        return transaction;
    }

    private static Customer CreateCustomer()
    {
        return new Customer
        {
            Id = 7,
            FullName = "Test Customer",
            Email = "customer@example.com",
            Phone = "0700000000",
            PasswordHash = "test",
            Status = CustomerStatus.Active,
            EmailVerified = true
        };
    }

    private static EventParking.Models.Entities.Event CreateEvent()
    {
        return new EventParking.Models.Entities.Event
        {
            Id = 3,
            Name = "Test Event",
            StartDateTime = DateTime.UtcNow.AddDays(1),
            EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            TicketPrice = 75m,
            ParkingFee = 20m,
            Capacity = 10
        };
    }

    private static Seat CreateSeat(
        int id,
        string number,
        decimal? priceOverride)
    {
        return new Seat
        {
            Id = id,
            EventId = 3,
            SeatNumber = number,
            PriceOverride = priceOverride,
            Status = SeatStatus.Available
        };
    }

    private static Booking CreatePendingBooking()
    {
        var customer = CreateCustomer();
        var eventItem = CreateEvent();
        var seat = CreateSeat(10, "A1", null);
        seat.Status = SeatStatus.Held;

        var booking = new Booking
        {
            Id = 44,
            BookingNumber = "BKG-2026-000044",
            CustomerId = customer.Id,
            Customer = customer,
            EventId = eventItem.Id,
            Event = eventItem,
            Status = BookingStatus.Pending,
            HoldExpiresAt = DateTime.UtcNow.AddMinutes(10),
            TotalAmount = 75m,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        booking.BookingSeats.Add(new BookingSeat
        {
            BookingId = booking.Id,
            Booking = booking,
            SeatId = seat.Id,
            Seat = seat,
            UnitPriceAtBooking = 75m
        });

        return booking;
    }
}
