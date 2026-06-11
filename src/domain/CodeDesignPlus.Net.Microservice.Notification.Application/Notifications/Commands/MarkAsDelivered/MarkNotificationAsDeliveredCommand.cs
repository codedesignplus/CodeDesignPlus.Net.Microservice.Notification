namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.MarkAsDelivered;

public record MarkNotificationAsDeliveredCommand(Guid NotificationId, Guid UserId) : IRequest<Unit>;

public class Validator : AbstractValidator<MarkNotificationAsDeliveredCommand>
{
    public Validator()
    {
        RuleFor(x => x.NotificationId).NotEmpty().NotNull();
        RuleFor(x => x.UserId).NotEmpty().NotNull();
    }
}
