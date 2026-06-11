using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;
using CodeDesignPlus.Net.Microservice.Notification.gRpc.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Services;

/// <summary>
/// SignalR implementation of INotificationDeliveryService.
/// Delivers notifications to specific connections via SignalR hub.
/// </summary>
public class SignalRNotificationDeliveryService(IHubContext<MainHub> hubContext) : INotificationDeliveryService
{
    public async Task DeliverToConnectionAsync(
        string connectionId,
        Guid notificationId,
        string eventName,
        string? payloadJson,
        CancellationToken cancellationToken)
    {
        var payload = string.IsNullOrEmpty(payloadJson)
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<object>(payloadJson);

        await hubContext.Clients.Client(connectionId).SendAsync(eventName, new
        {
            notificationId,
            eventName,
            payload
        }, cancellationToken);
    }
}
