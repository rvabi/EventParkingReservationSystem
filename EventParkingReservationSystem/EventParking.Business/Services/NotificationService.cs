using EventParking.Business.DTOs.Notifications;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public sealed class NotificationService : INotificationService
{
    private readonly IBookingRepository _bookingRepository;

    public NotificationService(
        IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<IReadOnlyList<NotificationResponse>>
        GetMyNotificationsAsync(
            int customerId,
            CancellationToken cancellationToken = default)
    {
        var notifications =
            await _bookingRepository
                .GetCustomerNotificationsAsync(
                    customerId,
                    cancellationToken);

        return notifications.Select(Map).ToList();
    }

    public async Task<bool> MarkAsReadAsync(
        int notificationId,
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var notification =
            await _bookingRepository
                .GetCustomerNotificationForUpdateAsync(
                    notificationId,
                    customerId,
                    cancellationToken);

        if (notification is null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            var utcNow = DateTime.UtcNow;
            notification.IsRead = true;
            notification.ReadAt = utcNow;
            notification.UpdatedAt = utcNow;

            await _bookingRepository.SaveChangesAsync(
                cancellationToken);
        }

        return true;
    }

    public async Task<int> NotifyEventUpdatedAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        var customerIds =
            await _bookingRepository.GetCustomerIdsForEventAsync(
                eventId,
                cancellationToken);

        if (customerIds.Count == 0)
        {
            return 0;
        }

        var utcNow = DateTime.UtcNow;

        var notifications = customerIds.Select(customerId =>
            new Notification
            {
                CustomerId = customerId,
                Type = NotificationType.EventUpdated,
                Title = "Event updated",
                Message =
                    "An event connected to one of your active bookings was updated. Review the latest event details.",
                CreatedAt = utcNow
            });

        await _bookingRepository.AddNotificationsAsync(
            notifications,
            cancellationToken);

        await _bookingRepository.SaveChangesAsync(
            cancellationToken);

        return customerIds.Count;
    }

    public async Task NotifyFoodReadyAsync(
        int customerId,
        string foodOrderNumber,
        CancellationToken cancellationToken = default)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException(
                "A valid customer ID is required.",
                nameof(customerId));
        }

        if (string.IsNullOrWhiteSpace(foodOrderNumber))
        {
            throw new ArgumentException(
                "A food order number is required.",
                nameof(foodOrderNumber));
        }

        var customer =
            await _bookingRepository.GetCustomerAsync(
                customerId,
                cancellationToken);

        if (customer is null)
        {
            throw new InvalidOperationException(
                "Customer not found.");
        }

        await _bookingRepository.AddNotificationsAsync(
            new[]
            {
                new Notification
                {
                    CustomerId = customerId,
                    Customer = customer,
                    Type = NotificationType.FoodReady,
                    Title = "Food order ready",
                    Message =
                        $"Food order {foodOrderNumber.Trim()} is ready for pickup.",
                    CreatedAt = DateTime.UtcNow
                }
            },
            cancellationToken);

        await _bookingRepository.SaveChangesAsync(
            cancellationToken);
    }

    private static NotificationResponse Map(
        Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type.ToString(),
            Title = notification.Title,
            Message = notification.Message,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            CreatedAt = notification.CreatedAt
        };
    }
}
