using EventParking.Business.DTOs.Bookings;
using EventParking.Business.DTOs.Payments;
using EventParking.Business.Interfaces;
using EventParking.Business.Options;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace EventParking.Business.Services;

public sealed class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly BookingOptions _options;

    public BookingService(
        IBookingRepository bookingRepository,
        BookingOptions options)
    {
        _bookingRepository = bookingRepository;
        _options = options;

        if (_options.HoldMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Booking hold duration must be greater than zero.");
        }
    }

    public async Task<BookingResponse> CreatePendingAsync(
        int customerId,
        CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException(
                "A valid authenticated customer is required.",
                nameof(customerId));
        }

        ArgumentNullException.ThrowIfNull(request);

        if (request.EventId <= 0)
        {
            throw new ArgumentException(
                "A valid event ID is required.",
                nameof(request));
        }

        if (request.SeatIds is null ||
            request.SeatIds.Count == 0)
        {
            throw new ArgumentException(
                "A booking must contain at least one seat.",
                nameof(request));
        }

        var seatIds = request.SeatIds
            .Where(seatId => seatId > 0)
            .Distinct()
            .OrderBy(seatId => seatId)
            .ToArray();

        if (seatIds.Length != request.SeatIds.Count)
        {
            throw new ArgumentException(
                "Seat IDs must be valid and cannot contain duplicates.",
                nameof(request));
        }

        if (request.ParkingSlotId is <= 0)
        {
            throw new ArgumentException(
                "ParkingSlotId must be null or a positive value.",
                nameof(request));
        }

        await using var transaction =
            await _bookingRepository
                .BeginSerializableTransactionAsync(
                    cancellationToken);

        var utcNow = DateTime.UtcNow;

        var customer =
            await _bookingRepository.GetCustomerAsync(
                customerId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Customer not found.");

        if (customer.Status != CustomerStatus.Active)
        {
            throw new UnauthorizedAccessException(
                "A deactivated customer cannot create bookings.");
        }

        if (!customer.EmailVerified)
        {
            throw new UnauthorizedAccessException(
                "Email verification is required before booking.");
        }

        var eventItem =
            await _bookingRepository.GetEventAsync(
                request.EventId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Event not found.");

        if (eventItem.StartDateTime <= utcNow)
        {
            throw new InvalidOperationException(
                "Bookings cannot be created for an event that has started or ended.");
        }

        var seats =
            await _bookingRepository.GetSeatsForUpdateAsync(
                eventItem.Id,
                seatIds,
                cancellationToken);

        if (seats.Count != seatIds.Length)
        {
            throw new InvalidOperationException(
                "One or more selected seats do not belong to the event.");
        }

        if (seats.Any(seat =>
                seat.Status != SeatStatus.Available))
        {
            throw new InvalidOperationException(
                "One or more selected seats are no longer available.");
        }

        ParkingSlot? parkingSlot = null;
        decimal parkingFee = 0m;

        if (request.ParkingSlotId.HasValue)
        {
            parkingSlot =
                await _bookingRepository
                    .GetParkingSlotForUpdateAsync(
                        eventItem.Id,
                        request.ParkingSlotId.Value,
                        cancellationToken)
                ?? throw new InvalidOperationException(
                    "The selected parking slot does not belong to the event.");

            if (parkingSlot.Status !=
                ParkingSlotStatus.Available)
            {
                throw new InvalidOperationException(
                    "The selected parking slot is no longer available.");
            }

            parkingFee = parkingSlot.FeeOverride
                ?? eventItem.ParkingFee;

            if (parkingFee < 0m)
            {
                throw new InvalidOperationException(
                    "Parking fee cannot be negative.");
            }
        }

        var bookingSeats = seats
            .Select(seat => new BookingSeat
            {
                SeatId = seat.Id,
                Seat = seat,
                UnitPriceAtBooking =
                    seat.PriceOverride ?? eventItem.TicketPrice,
                CreatedAt = utcNow
            })
            .ToList();

        if (bookingSeats.Any(bookingSeat =>
                bookingSeat.UnitPriceAtBooking < 0m))
        {
            throw new InvalidOperationException(
                "Seat price cannot be negative.");
        }

        foreach (var seat in seats)
        {
            seat.Status = SeatStatus.Held;
            seat.UpdatedAt = utcNow;
        }

        if (parkingSlot is not null)
        {
            parkingSlot.Status = ParkingSlotStatus.Held;
            parkingSlot.UpdatedAt = utcNow;
        }

        var booking = new Booking
        {
            BookingNumber =
                $"TMP-{Guid.NewGuid().ToString("N")[..20]}",
            CustomerId = customer.Id,
            Customer = customer,
            EventId = eventItem.Id,
            Event = eventItem,
            Status = BookingStatus.Pending,
            HoldExpiresAt =
                utcNow.AddMinutes(_options.HoldMinutes),
            TotalAmount =
                bookingSeats.Sum(bookingSeat =>
                    bookingSeat.UnitPriceAtBooking) +
                parkingFee,
            CreatedAt = utcNow,
            BookingSeats = bookingSeats
        };

        if (parkingSlot is not null)
        {
            booking.ParkingReservation =
                new ParkingReservation
                {
                    ParkingSlotId = parkingSlot.Id,
                    ParkingSlot = parkingSlot,
                    FeeAtReservation = parkingFee,
                    IsActive = true,
                    CreatedAt = utcNow
                };
        }

        await _bookingRepository.AddBookingAsync(
            booking,
            cancellationToken);

        await _bookingRepository.SaveChangesAsync(
            cancellationToken);

        booking.BookingNumber =
            $"BKG-{utcNow:yyyy}-{booking.Id:D6}";
        booking.UpdatedAt = utcNow;

        await _bookingRepository.AddNotificationsAsync(
            new[]
            {
                CreateNotification(
                    customer.Id,
                    NotificationType.BookingCreated,
                    "Booking held",
                    $"Booking {booking.BookingNumber} is held until " +
                    $"{booking.HoldExpiresAt:O}.",
                    utcNow)
            },
            cancellationToken);

        await _bookingRepository.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return MapBooking(booking, utcNow);
    }

    public async Task<BookingResponse?> GetByIdAsync(
        int bookingId,
        int requesterCustomerId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var booking =
            await _bookingRepository.GetBookingDetailsAsync(
                bookingId,
                cancellationToken);

        if (booking is null)
        {
            return null;
        }

        EnsureOwnership(
            booking,
            requesterCustomerId,
            isAdministrator);

        return MapBooking(booking, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<BookingResponse>>
        GetMyBookingsAsync(
            int customerId,
            CancellationToken cancellationToken = default)
    {
        var bookings =
            await _bookingRepository.GetCustomerBookingsAsync(
                customerId,
                cancellationToken);

        var utcNow = DateTime.UtcNow;

        return bookings
            .Select(booking => MapBooking(booking, utcNow))
            .ToList();
    }

    public async Task<IReadOnlyList<BookingResponse>> GetAllAsync(
        int? eventId,
        BookingStatus? status,
        CancellationToken cancellationToken = default)
    {
        var bookings =
            await _bookingRepository.GetBookingsAsync(
                eventId,
                status,
                cancellationToken);

        var utcNow = DateTime.UtcNow;

        return bookings
            .Select(booking => MapBooking(booking, utcNow))
            .ToList();
    }

    public async Task<HoldStatusResponse?> GetHoldStatusAsync(
        int bookingId,
        int requesterCustomerId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var booking =
            await _bookingRepository.GetBookingDetailsAsync(
                bookingId,
                cancellationToken);

        if (booking is null)
        {
            return null;
        }

        EnsureOwnership(
            booking,
            requesterCustomerId,
            isAdministrator);

        var utcNow = DateTime.UtcNow;
        var remainingSeconds =
            GetRemainingSeconds(booking, utcNow);

        return new HoldStatusResponse
        {
            BookingId = booking.Id,
            BookingNumber = booking.BookingNumber,
            Status = booking.Status.ToString(),
            HoldExpiresAt = booking.HoldExpiresAt,
            RemainingSeconds = remainingSeconds,
            CanPay =
                booking.Status == BookingStatus.Pending &&
                remainingSeconds > 0
        };
    }

    public async Task<bool> CancelPendingAsync(
        int bookingId,
        int requesterCustomerId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _bookingRepository
                .BeginSerializableTransactionAsync(
                    cancellationToken);

        var booking =
            await _bookingRepository.GetTrackedBookingAsync(
                bookingId,
                cancellationToken);

        if (booking is null)
        {
            return false;
        }

        EnsureOwnership(
            booking,
            requesterCustomerId,
            isAdministrator);

        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only an unpaid pending booking can be cancelled.");
        }

        var utcNow = DateTime.UtcNow;

        if (!booking.HoldExpiresAt.HasValue ||
            booking.HoldExpiresAt.Value <= utcNow)
        {
            await ExpireTrackedBookingAsync(
                booking,
                utcNow,
                cancellationToken);
            await _bookingRepository.SaveChangesAsync(
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            throw new InvalidOperationException(
                "The booking hold has expired and cannot be cancelled.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = utcNow;
        booking.UpdatedAt = utcNow;

        ReleaseResources(booking, utcNow);

        await _bookingRepository.AddNotificationsAsync(
            new[]
            {
                CreateNotification(
                    booking.CustomerId,
                    NotificationType.BookingCancelled,
                    "Booking cancelled",
                    $"Booking {booking.BookingNumber} was cancelled and its held resources were released.",
                    utcNow)
            },
            cancellationToken);

        await _bookingRepository.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<PaymentResponse?> GetPaymentAsync(
        int bookingId,
        int requesterCustomerId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var booking =
            await _bookingRepository.GetBookingDetailsAsync(
                bookingId,
                cancellationToken);

        if (booking is null)
        {
            return null;
        }

        EnsureOwnership(
            booking,
            requesterCustomerId,
            isAdministrator);

        return booking.Payment is null
            ? new PaymentResponse
            {
                BookingId = booking.Id,
                BookingNumber = booking.BookingNumber,
                CustomerId = booking.CustomerId,
                Amount = booking.TotalAmount,
                BookingStatus = booking.Status.ToString(),
                Status = PaymentStatus.Pending.ToString(),
                CreatedAt = booking.CreatedAt
            }
            : MapPayment(booking.Payment);
    }

    public async Task<PaymentResponse> SimulatePaymentAsync(
        int bookingId,
        int customerId,
        SimulatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction =
            await _bookingRepository
                .BeginSerializableTransactionAsync(
                    cancellationToken);

        var booking =
            await _bookingRepository.GetTrackedBookingAsync(
                bookingId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Booking not found.");

        if (booking.CustomerId != customerId)
        {
            throw new UnauthorizedAccessException(
                "The booking does not belong to the authenticated customer.");
        }

        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException(
                booking.Status == BookingStatus.Confirmed
                    ? "This booking has already been paid."
                    : "Only a pending booking can be paid.");
        }

        var utcNow = DateTime.UtcNow;

        if (!booking.HoldExpiresAt.HasValue ||
            booking.HoldExpiresAt.Value <= utcNow)
        {
            await ExpireTrackedBookingAsync(
                booking,
                utcNow,
                cancellationToken);
            await _bookingRepository.SaveChangesAsync(
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            throw new InvalidOperationException(
                "The booking hold expired before payment completed.");
        }

        var payment = booking.Payment;

        if (payment?.Status == PaymentStatus.Completed)
        {
            throw new InvalidOperationException(
                "A completed payment already exists for this booking.");
        }

        if (payment is null)
        {
            payment = new Payment
            {
                BookingId = booking.Id,
                Booking = booking,
                CustomerId = booking.CustomerId,
                Customer = booking.Customer,
                Amount = booking.TotalAmount,
                CreatedAt = utcNow
            };

            booking.Payment = payment;
        }

        payment.Amount = booking.TotalAmount;
        payment.TransactionReference =
            $"PAY-{utcNow:yyyyMMddHHmmss}-" +
            Guid.NewGuid().ToString("N")[..8]
                .ToUpperInvariant();
        payment.UpdatedAt = utcNow;

        if (!request.SimulateSuccess)
        {
            payment.Status = PaymentStatus.Failed;
            payment.PaidAt = null;

            await _bookingRepository.SaveChangesAsync(
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return MapPayment(payment);
        }

        payment.Status = PaymentStatus.Completed;
        payment.PaidAt = utcNow;

        booking.Status = BookingStatus.Confirmed;
        booking.HoldExpiresAt = null;
        booking.UpdatedAt = utcNow;

        foreach (var bookingSeat in booking.BookingSeats)
        {
            bookingSeat.Seat.Status = SeatStatus.Booked;
            bookingSeat.Seat.UpdatedAt = utcNow;
        }

        if (booking.ParkingReservation is
            { IsActive: true } reservation)
        {
            reservation.ParkingSlot.Status =
                ParkingSlotStatus.Reserved;
            reservation.ParkingSlot.UpdatedAt = utcNow;
            reservation.UpdatedAt = utcNow;
        }

        await _bookingRepository.AddNotificationsAsync(
            new[]
            {
                CreateNotification(
                    booking.CustomerId,
                    NotificationType.PaymentCompleted,
                    "Payment completed",
                    $"Payment completed and booking {booking.BookingNumber} is confirmed.",
                    utcNow)
            },
            cancellationToken);

        await _bookingRepository.SaveChangesAsync(
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return MapPayment(payment);
    }

    public async Task<IReadOnlyList<PaymentResponse>>
        GetMyPaymentsAsync(
            int customerId,
            CancellationToken cancellationToken = default)
    {
        var payments =
            await _bookingRepository.GetCustomerPaymentsAsync(
                customerId,
                cancellationToken);

        return payments.Select(MapPayment).ToList();
    }

    public async Task<byte[]?> GenerateReceiptPdfAsync(
        int paymentId,
        int requesterCustomerId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var payment =
            await _bookingRepository.GetPaymentDetailsAsync(
                paymentId,
                cancellationToken);

        if (payment is null)
        {
            return null;
        }

        if (!isAdministrator &&
            payment.CustomerId != requesterCustomerId)
        {
            throw new UnauthorizedAccessException(
                "The payment does not belong to the authenticated customer.");
        }

        if (payment.Status != PaymentStatus.Completed ||
            !payment.PaidAt.HasValue)
        {
            throw new InvalidOperationException(
                "A receipt is available only for a completed payment.");
        }

        return CreateReceiptPdf(payment);
    }

    public async Task<int> ExpirePendingBookingsAsync(
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var bookingIds =
            await _bookingRepository
                .GetExpiredPendingBookingIdsAsync(
                    utcNow,
                    cancellationToken);

        var expiredCount = 0;

        foreach (var bookingId in bookingIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var transaction =
                await _bookingRepository
                    .BeginSerializableTransactionAsync(
                        cancellationToken);

            var booking =
                await _bookingRepository.GetTrackedBookingAsync(
                    bookingId,
                    cancellationToken);

            if (booking is null ||
                booking.Status != BookingStatus.Pending ||
                !booking.HoldExpiresAt.HasValue ||
                booking.HoldExpiresAt.Value > utcNow)
            {
                await transaction.CommitAsync(
                    cancellationToken);
                continue;
            }

            await ExpireTrackedBookingAsync(
                booking,
                utcNow,
                cancellationToken);

            await _bookingRepository.SaveChangesAsync(
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            expiredCount++;
        }

        return expiredCount;
    }

    public Task<bool> HasActiveFutureBookingAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        return _bookingRepository.HasActiveFutureBookingAsync(
            customerId,
            DateTime.UtcNow,
            cancellationToken);
    }

    private async Task ExpireTrackedBookingAsync(
        Booking booking,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        booking.Status = BookingStatus.Expired;
        booking.UpdatedAt = utcNow;

        ReleaseResources(booking, utcNow);

        await _bookingRepository.AddNotificationsAsync(
            new[]
            {
                CreateNotification(
                    booking.CustomerId,
                    NotificationType.BookingExpired,
                    "Booking hold expired",
                    $"Booking {booking.BookingNumber} expired and its held resources were released.",
                    utcNow)
            },
            cancellationToken);
    }

    private static void ReleaseResources(
        Booking booking,
        DateTime utcNow)
    {
        foreach (var bookingSeat in booking.BookingSeats)
        {
            bookingSeat.Seat.Status = SeatStatus.Available;
            bookingSeat.Seat.UpdatedAt = utcNow;
        }

        if (booking.ParkingReservation is
            { IsActive: true } reservation)
        {
            reservation.IsActive = false;
            reservation.UpdatedAt = utcNow;
            reservation.ParkingSlot.Status =
                ParkingSlotStatus.Available;
            reservation.ParkingSlot.UpdatedAt = utcNow;
        }
    }

    private static Notification CreateNotification(
        int customerId,
        NotificationType type,
        string title,
        string message,
        DateTime utcNow)
    {
        return new Notification
        {
            CustomerId = customerId,
            Type = type,
            Title = title,
            Message = message,
            CreatedAt = utcNow
        };
    }

    private static void EnsureOwnership(
        Booking booking,
        int requesterCustomerId,
        bool isAdministrator)
    {
        if (!isAdministrator &&
            booking.CustomerId != requesterCustomerId)
        {
            throw new UnauthorizedAccessException(
                "The booking does not belong to the authenticated customer.");
        }
    }

    private static int GetRemainingSeconds(
        Booking booking,
        DateTime utcNow)
    {
        if (booking.Status != BookingStatus.Pending ||
            !booking.HoldExpiresAt.HasValue)
        {
            return 0;
        }

        return Math.Max(
            0,
            (int)Math.Ceiling(
                (booking.HoldExpiresAt.Value - utcNow)
                    .TotalSeconds));
    }

    private static BookingResponse MapBooking(
        Booking booking,
        DateTime utcNow)
    {
        var seatResponses = booking.BookingSeats
            .OrderBy(bookingSeat => bookingSeat.Seat.SeatNumber)
            .Select(bookingSeat => new BookingSeatResponse
            {
                SeatId = bookingSeat.SeatId,
                SeatNumber = bookingSeat.Seat.SeatNumber,
                UnitPriceAtBooking =
                    bookingSeat.UnitPriceAtBooking
            })
            .ToList();

        var parking = booking.ParkingReservation is null
            ? null
            : new BookingParkingResponse
            {
                ParkingSlotId =
                    booking.ParkingReservation.ParkingSlotId,
                SlotNumber =
                    booking.ParkingReservation.ParkingSlot
                        .SlotNumber,
                Zone =
                    booking.ParkingReservation.ParkingSlot.Zone,
                FeeAtReservation =
                    booking.ParkingReservation.FeeAtReservation
            };

        return new BookingResponse
        {
            Id = booking.Id,
            BookingNumber = booking.BookingNumber,
            CustomerId = booking.CustomerId,
            CustomerName = booking.Customer.FullName,
            EventId = booking.EventId,
            EventName = booking.Event.Name,
            EventStartDateTime = booking.Event.StartDateTime,
            Status = booking.Status.ToString(),
            HoldExpiresAt = booking.HoldExpiresAt,
            RemainingHoldSeconds =
                GetRemainingSeconds(booking, utcNow),
            SeatTotal = seatResponses.Sum(seat =>
                seat.UnitPriceAtBooking),
            ParkingTotal = parking?.FeeAtReservation ?? 0m,
            TotalAmount = booking.TotalAmount,
            Seats = seatResponses,
            Parking = parking,
            PaymentStatus = booking.Payment?.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            CancelledAt = booking.CancelledAt
        };
    }

    private static PaymentResponse MapPayment(Payment payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            BookingId = payment.BookingId,
            BookingNumber = payment.Booking.BookingNumber,
            CustomerId = payment.CustomerId,
            Amount = payment.Amount,
            BookingStatus = payment.Booking.Status.ToString(),
            Status = payment.Status.ToString(),
            TransactionReference =
                payment.TransactionReference,
            PaidAt = payment.PaidAt,
            CreatedAt = payment.CreatedAt
        };
    }

    private static byte[] CreateReceiptPdf(Payment payment)
    {
        using var document = new PdfDocument();
        document.Info.Title =
            $"Receipt {payment.TransactionReference}";

        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;

        using var graphics = XGraphics.FromPdfPage(page);

        var titleFont = new XFont(
            "Arial",
            20,
            XFontStyleEx.Bold);
        var headingFont = new XFont(
            "Arial",
            11,
            XFontStyleEx.Bold);
        var bodyFont = new XFont("Arial", 10);

        var y = 55d;

        graphics.DrawString(
            "Event & Parking Reservation System",
            titleFont,
            XBrushes.DarkBlue,
            new XRect(45, y, page.Width.Point - 90, 30),
            XStringFormats.TopLeft);

        y += 42;

        graphics.DrawString(
            "PAYMENT RECEIPT",
            headingFont,
            XBrushes.Black,
            new XRect(45, y, page.Width.Point - 90, 20),
            XStringFormats.TopLeft);

        y += 32;

        var booking = payment.Booking;

        DrawReceiptLine(graphics, bodyFont, "Receipt reference", payment.TransactionReference ?? "-", ref y);
        DrawReceiptLine(graphics, bodyFont, "Payment date (UTC)", payment.PaidAt!.Value.ToString("yyyy-MM-dd HH:mm:ss"), ref y);
        DrawReceiptLine(graphics, bodyFont, "Booking number", booking.BookingNumber, ref y);
        DrawReceiptLine(graphics, bodyFont, "Customer", payment.Customer.FullName, ref y);
        DrawReceiptLine(graphics, bodyFont, "Event", booking.Event.Name, ref y);
        DrawReceiptLine(graphics, bodyFont, "Event date (UTC)", booking.Event.StartDateTime.ToString("yyyy-MM-dd HH:mm:ss"), ref y);

        y += 10;
        graphics.DrawString(
            "Seats",
            headingFont,
            XBrushes.Black,
            new XPoint(45, y));
        y += 20;

        foreach (var bookingSeat in booking.BookingSeats
                     .OrderBy(item => item.Seat.SeatNumber))
        {
            DrawReceiptLine(
                graphics,
                bodyFont,
                bookingSeat.Seat.SeatNumber,
                bookingSeat.UnitPriceAtBooking.ToString("0.00"),
                ref y);
        }

        if (booking.ParkingReservation is not null)
        {
            y += 8;
            DrawReceiptLine(
                graphics,
                bodyFont,
                $"Parking {booking.ParkingReservation.ParkingSlot.SlotNumber}",
                booking.ParkingReservation.FeeAtReservation.ToString("0.00"),
                ref y);
        }

        y += 14;
        graphics.DrawLine(
            XPens.Gray,
            45,
            y,
            page.Width.Point - 45,
            y);
        y += 22;

        DrawReceiptLine(
            graphics,
            headingFont,
            "Total paid",
            payment.Amount.ToString("0.00"),
            ref y);

        y += 30;
        graphics.DrawString(
            "This receipt records a simulated student-project payment.",
            bodyFont,
            XBrushes.Gray,
            new XPoint(45, y));

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static void DrawReceiptLine(
        XGraphics graphics,
        XFont font,
        string label,
        string value,
        ref double y)
    {
        graphics.DrawString(
            label,
            font,
            XBrushes.Black,
            new XPoint(45, y));

        graphics.DrawString(
            value,
            font,
            XBrushes.Black,
            new XRect(250, y - 12, 290, 18),
            XStringFormats.TopRight);

        y += 20;
    }
}
