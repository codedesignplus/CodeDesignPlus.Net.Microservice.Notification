namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.MarkAsDelivered;

public class MarkNotificationAsDeliveredCommandHandler(INotificationsRepository repository)
    : IRequestHandler<MarkNotificationAsDeliveredCommand, Unit>
{
    public async Task<Unit> Handle(MarkNotificationAsDeliveredCommand request, CancellationToken cancellationToken)
    {
        var notification = await repository.FindAsync<NotificationsAggregate>(request.NotificationId, cancellationToken);

        ApplicationGuard.IsNull(notification, Errors.NotificationNotFound);

        notification!.MarkAsDelivered(SystemClock.Instance.GetCurrentInstant(), request.UserId);

        await repository.UpdateAsync(notification, cancellationToken);

        return Unit.Value;
    }
}
