namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.MarkAsRead;

/// <summary>
/// Deja constancia de que el lector vio un aviso.
/// </summary>
/// <param name="NotificationId">El aviso leido.</param>
public record MarkAsReadCommand(Guid NotificationId) : IRequest;

/// <summary>Validador de <see cref="MarkAsReadCommand"/>.</summary>
public class Validator : AbstractValidator<MarkAsReadCommand>
{
    /// <summary>Inicializa las reglas de validacion.</summary>
    public Validator() => RuleFor(x => x.NotificationId).NotEmpty();
}
