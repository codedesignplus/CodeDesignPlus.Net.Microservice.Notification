namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.BroadcastNotification;

[DtoGenerator]
public record BroadcastNotificationCommand(
    Guid Id, 
    string MethodName, 
    string JsonPayload, 
    string TraceId
) : IRequest<bool>;

public class BroadcastValidator : AbstractValidator<BroadcastNotificationCommand>
{
    public BroadcastValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TraceId).NotEmpty();

        RuleFor(x => x.MethodName).NotEmpty();
        RuleFor(x => x.JsonPayload).NotEmpty();
        
        RuleFor(x => x.JsonPayload)
            .Must(json => json.TrimStart().StartsWith('{') || json.TrimStart().StartsWith('['))
            .WithMessage("The payload must be a valid JSON.");
    }
}