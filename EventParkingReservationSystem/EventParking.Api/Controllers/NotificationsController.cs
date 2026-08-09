using System.Security.Claims;
using EventParking.Business.DTOs.Notifications;
using EventParking.Business.Interfaces;
using EventParking.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Customer))]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(
        INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("my-notifications")]
    public async Task<
        ActionResult<IReadOnlyList<NotificationResponse>>>
        GetMyNotifications(
            CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
        {
            return Unauthorized();
        }

        return Ok(await _notificationService
            .GetMyNotificationsAsync(
                customerId.Value,
                cancellationToken));
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(
        int id,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
        {
            return Unauthorized();
        }

        var updated =
            await _notificationService.MarkAsReadAsync(
                id,
                customerId.Value,
                cancellationToken);

        return updated
            ? NoContent()
            : NotFound(new
            {
                message = "Notification not found."
            });
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
