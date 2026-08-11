using System.Security.Claims;
using EventParking.Business.DTOs.Bookings;
using EventParking.Business.DTOs.Payments;
using EventParking.Business.Interfaces;
using EventParking.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<ActionResult<BookingResponse>> Create(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var booking =
                await _bookingService.CreatePendingAsync(
                    customerId.Value,
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = booking.Id },
                booking);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var booking = await _bookingService.GetByIdAsync(
                id,
                customerId.Value,
                IsAdministrator(),
                cancellationToken);

            return booking is null
                ? NotFound(new { message = "Booking not found." })
                : Ok(booking);
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = exception.Message });
        }
    }

    [HttpGet("my-bookings")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<
        ActionResult<IReadOnlyList<BookingResponse>>>
        GetMyBookings(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
        {
            return Unauthorized();
        }

        return Ok(await _bookingService.GetMyBookingsAsync(
            customerId.Value,
            cancellationToken));
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<
        ActionResult<IReadOnlyList<BookingResponse>>>
        GetAll(
            [FromQuery] int? eventId,
            [FromQuery] BookingStatus? status,
            CancellationToken cancellationToken)
    {
        if (eventId is <= 0)
        {
            return BadRequest(new
            {
                message = "eventId must be null or a positive value."
            });
        }

        return Ok(await _bookingService.GetAllAsync(
            eventId,
            status,
            cancellationToken));
    }

    [HttpGet("{id:int}/hold-status")]
    public async Task<ActionResult<HoldStatusResponse>>
        GetHoldStatus(
            int id,
            CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var status =
                await _bookingService.GetHoldStatusAsync(
                    id,
                    customerId.Value,
                    IsAdministrator(),
                    cancellationToken);

            return status is null
                ? NotFound(new { message = "Booking not found." })
                : Ok(status);
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = exception.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(
        int id,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var cancelled =
                await _bookingService.CancelPendingAsync(
                    id,
                    customerId.Value,
                    IsAdministrator(),
                    cancellationToken);

            return cancelled
                ? NoContent()
                : NotFound(new { message = "Booking not found." });
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("{id:int}/payment")]
    public async Task<ActionResult<PaymentResponse>> GetPayment(
        int id,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var payment =
                await _bookingService.GetPaymentAsync(
                    id,
                    customerId.Value,
                    IsAdministrator(),
                    cancellationToken);

            return payment is null
                ? NotFound(new { message = "Booking not found." })
                : Ok(payment);
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = exception.Message });
        }
    }

    [HttpPost("{id:int}/payment")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<ActionResult<PaymentResponse>>
        SimulatePayment(
            int id,
            [FromBody] SimulatePaymentRequest request,
            CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var payment =
                await _bookingService.SimulatePaymentAsync(
                    id,
                    customerId.Value,
                    request,
                    cancellationToken);

            return Ok(payment);
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    private int? GetCustomerId()
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var customerId)
            ? customerId
            : null;
    }

    private bool IsAdministrator()
    {
        return User.IsInRole(nameof(UserRole.Administrator));
    }
}
