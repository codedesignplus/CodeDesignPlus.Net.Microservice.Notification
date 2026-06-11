using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.MarkAsDelivered;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Repositories;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.DeliverPendingNotifications;

/// <summary>
/// Handler that delivers pending notifications (Type=User) to a connected user via SignalR.
/// Executed in background via Hangfire - does not block SignalR connection.
/// Uses INotificationDeliveryService abstraction to avoid direct SignalR dependency in Application layer.
/// </summary>
public class DeliverPendingNotificationsCommandHandler(
    IMediator mediator,
    INotificationDeliveryService deliveryService,
    ILogger<DeliverPendingNotificationsCommandHandler> logger,
    INotificationsRepository repository) : IRequestHandler<DeliverPendingNotificationsCommand, Unit>
{

    public async Task<Unit> Handle(DeliverPendingNotificationsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting pending notifications delivery for user {UserId}, connection {ConnectionId}", request.UserId, request.ConnectionId);

            var pending = await repository.GetPendingByUserIdAsync(request.UserId, cancellationToken);

            if (pending == null || pending.Count == 0)
            {
                logger.LogInformation("No pending notifications for user {UserId}", request.UserId);
                return Unit.Value;
            }

            logger.LogInformation("Delivering {Count} pending notifications to user {UserId}", pending.Count, request.UserId);

            foreach (var notification in pending)
            {
                try
                {
                    await deliveryService.DeliverToConnectionAsync(
                        request.ConnectionId,
                        notification.Id,
                        notification.EventName!,
                        notification.PayloadPreview,
                        cancellationToken);

                    await mediator.Send(new MarkNotificationAsDeliveredCommand(notification.Id, request.UserId), cancellationToken);

                    logger.LogDebug("Delivered notification {NotificationId} to user {UserId}", notification.Id, request.UserId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to deliver notification {NotificationId} to user {UserId}", notification.Id, request.UserId);
                }
            }

            logger.LogInformation("Successfully delivered {Count} pending notifications to user {UserId}", pending.Count, request.UserId);

            return Unit.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error delivering pending notifications to user {UserId}, connection {ConnectionId}", request.UserId, request.ConnectionId);
            throw;
        }
    }
}
