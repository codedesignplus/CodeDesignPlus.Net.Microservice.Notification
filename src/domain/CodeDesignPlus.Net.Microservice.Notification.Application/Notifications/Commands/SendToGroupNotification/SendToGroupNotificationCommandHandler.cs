using CodeDesignPlus.Net.Microservice.Notification.Domain.Constants;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Enums;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.SendToGroupNotification;


public class SendToGroupNotificationCommandHandler(INotifierGateway notifier, INotificationsRepository repository)
    : IRequestHandler<SendToGroupNotificationCommand, bool>
{
    public async Task<bool> Handle(SendToGroupNotificationCommand request, CancellationToken cancellationToken)
    {
        var aggregate = NotificationsAggregate.Create(request.Id, request.GroupName, request.EventName, NotificationType.Group, request.JsonPayload, request.Tenant, request.SentBy);

        try
        {
            var qualifiedGroup = GroupConstants.BuildTenantGroupName(request.Tenant, request.GroupName);

            await notifier.SendToGroupAsync(qualifiedGroup, request.EventName, request.JsonPayload, cancellationToken);

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