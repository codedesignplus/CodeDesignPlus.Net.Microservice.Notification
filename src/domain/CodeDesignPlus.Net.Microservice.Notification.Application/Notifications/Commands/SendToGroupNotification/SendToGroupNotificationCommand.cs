namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.SendToGroupNotification;

[DtoGenerator]
public record SendToGroupNotificationCommand(
    Guid Id, 
    string GroupName, 
    string MethodName, 
    string JsonPayload, 
    string TraceId
) : IRequest<bool>;

public class SendToGroupValidator : AbstractValidator<SendToGroupNotificationCommand>
{
    public SendToGroupValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TraceId).NotEmpty();

        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("You must specify a GroupName.");

        RuleFor(x => x.MethodName).NotEmpty();
        RuleFor(x => x.JsonPayload).NotEmpty();
        
        RuleFor(x => x.JsonPayload)
            .Must(json => json.TrimStart().StartsWith('{') || json.TrimStart().StartsWith('['))
            .WithMessage("The payload must be a valid JSON.");
    }
}