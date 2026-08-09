using EventParking.Business.DTOs.Notifications;

namespace EventParking.Business.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationResponse>> GetMyNotificationsAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(
        int notificationId,
        int customerId,
        CancellationToken cancellationToken = default);

    Task<int> NotifyEventUpdatedAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task NotifyFoodReadyAsync(
        int customerId,
        string foodOrderNumber,
        CancellationToken cancellationToken = default);
}
