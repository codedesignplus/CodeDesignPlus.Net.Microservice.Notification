namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.SendToUserNotification;

[DtoGenerator]
public record SendToUserNotificationCommand(Guid Id, Guid UserId, string EventName, string JsonPayload, Guid Tenant, Guid SentBy) : IRequest<bool>;

public class Validator : AbstractValidator<SendToUserNotificationCommand>
{
    public Validator()
    {
        RuleFor(x => x.Id).NotEmpty().NotNull();
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.EventName)
            .NotEmpty();

        RuleFor(x => x.JsonPayload)
            .NotEmpty();

        RuleFor(x => x.JsonPayload)
            .Must(json => json.TrimStart().StartsWith('{') || json.TrimStart().StartsWith('['))
            .WithErrorCode(Errors.PayloadIsNotValidJson.Code);

        RuleFor(x => x.Tenant).NotEmpty();
    }
}
