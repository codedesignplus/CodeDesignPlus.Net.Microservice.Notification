namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.DeliverPendingNotifications;

/// <summary>
/// Command to deliver pending notifications (Type=User) to a connected user.
/// Executed in background via Hangfire when a user connects to SignalR.
/// </summary>
/// <param name="UserId">ID of the user who connected</param>
/// <param name="ConnectionId">SignalR connection ID</param>
public record DeliverPendingNotificationsCommand(Guid UserId, string ConnectionId) : IRequest<Unit>;
