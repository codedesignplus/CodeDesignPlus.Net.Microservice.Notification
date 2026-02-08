using CodeDesignPlus.Net.Microservice.Notification.Domain.Enums;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.SendToUserNotification;

public class SendToUserNotificationCommandHandler(INotifierGateway notifier, INotificationsRepository repository) 
    : IRequestHandler<SendToUserNotificationCommand, bool>
{
    public async Task<bool> Handle(SendToUserNotificationCommand request, CancellationToken cancellationToken)
    {
        var aggregate = NotificationsAggregate.Create(request.Id, request.UserId, NotificationType.User, request.JsonPayload, request.Tenant, request.SentBy);

        try
        {
            await notifier.SendToUserAsync(request.UserId, request.EventName, request.JsonPayload, cancellationToken);

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