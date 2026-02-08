using CodeDesignPlus.Net.Microservice.Notification.Domain.Enums;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.BroadcastNotification;

public class BroadcastNotificationCommandHandler(INotifierGateway notifier, INotificationsRepository repository) : IRequestHandler<BroadcastNotificationCommand, bool>
{
    public async Task<bool> Handle(BroadcastNotificationCommand request, CancellationToken cancellationToken)
    {
        var aggregate = NotificationsAggregate.Create(request.Id, NotificationType.Broadcast, request.JsonPayload, request.Tenant, request.SentBy);

        try
        {
            await notifier.BroadcastAsync(request.EventName, request.JsonPayload, cancellationToken);

            aggregate.MarkAsSent(Guid.Empty);
        }
        catch (Exception ex)
        {
            aggregate.MarkAsFailed(ex.Message, Guid.Empty);
        }

        await repository.CreateAsync(aggregate, cancellationToken);

        return aggregate.WasSuccess;
    }
}