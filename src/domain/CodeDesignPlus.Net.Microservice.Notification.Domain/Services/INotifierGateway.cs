using System;

namespace CodeDesignPlus.Net.Microservice.Notification.Domain.Services;

public interface INotifierGateway
{
    Task SendToUserAsync(Guid userId, string method, string payload, CancellationToken cancellationToken);
    Task BroadcastAsync(string method, string payload, CancellationToken cancellationToken);
    Task SendToGroupAsync(string group, string method, string payload, CancellationToken cancellationToken);
}