namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.SendToUserNotification;

[DtoGenerator]
public record SendToUserNotificationCommand(Guid Id, string UserId, string MethodName, string JsonPayload, string TraceId) : IRequest<bool>;

public class Validator : AbstractValidator<SendToUserNotificationCommand>
{
    public Validator()
    {
        RuleFor(x => x.Id).NotEmpty().NotNull();
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("The UserId is required for sending direct notifications.");

        RuleFor(x => x.MethodName)
            .NotEmpty().WithMessage("The MethodName is required so the client knows which event to listen to.");

        RuleFor(x => x.JsonPayload)
            .NotEmpty().WithMessage("The payload cannot be empty.");

        RuleFor(x => x.JsonPayload)
            .Must(json => json.TrimStart().StartsWith('{') || json.TrimStart().StartsWith('['))
            .WithMessage("The payload must be a valid JSON.");
    }
}
