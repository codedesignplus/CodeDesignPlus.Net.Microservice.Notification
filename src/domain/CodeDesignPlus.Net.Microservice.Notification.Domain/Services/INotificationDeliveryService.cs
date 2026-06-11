namespace CodeDesignPlus.Net.Microservice.Notification.Domain.Services;

/// <summary>
/// Service for delivering notifications to specific SignalR connections.
/// Abstraction to avoid direct SignalR dependencies in Application layer.
/// </summary>
public interface INotificationDeliveryService
{
    /// <summary>
    /// Delivers a notification to a specific SignalR connection.
    /// </summary>
    /// <param name="connectionId">SignalR connection ID</param>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="eventName">Event name (e.g., "OrderPaymentCompleted")</param>
    /// <param name="payloadJson">JSON payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeliverToConnectionAsync(
        string connectionId,
        Guid notificationId,
        string eventName,
        string? payloadJson,
        CancellationToken cancellationToken);
}
