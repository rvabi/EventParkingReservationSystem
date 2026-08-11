using System.Security.Claims;
using EventParking.Business.DTOs.Payments;
using EventParking.Business.Interfaces;
using EventParking.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public PaymentsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("my-payments")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<
        ActionResult<IReadOnlyList<PaymentResponse>>>
        GetMyPayments(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
        {
            return Unauthorized();
        }

        return Ok(await _bookingService.GetMyPaymentsAsync(
            customerId.Value,
            cancellationToken));
    }

    [HttpGet("{id:int}/receipt")]
    public async Task<IActionResult> DownloadReceipt(
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
            var receipt =
                await _bookingService.GenerateReceiptPdfAsync(
                    id,
                    customerId.Value,
                    User.IsInRole(
                        nameof(UserRole.Administrator)),
                    cancellationToken);

            return receipt is null
                ? NotFound(new { message = "Payment not found." })
                : File(
                    receipt,
                    "application/pdf",
                    $"payment-{id}-receipt.pdf");
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

    private int? GetCustomerId()
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var customerId)
            ? customerId
            : null;
    }
}
