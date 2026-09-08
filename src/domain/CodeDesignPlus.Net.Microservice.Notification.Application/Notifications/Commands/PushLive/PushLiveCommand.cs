namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.PushLive;

/// <summary>
/// Empuja un mensaje efimero por SignalR. **No persiste nada.**
/// </summary>
/// <remarks>
/// Progreso, sala en vivo, turnos de palabra: cosas que pierden valor en segundos. Antes se guardaban
/// igual que un aviso, y un reparto de cuota extraordinaria de 200 unidades dejaba 200 documentos en
/// Mongo que nadie leyo jamas.
/// </remarks>
/// <param name="Tenant">La copropiedad a la que pertenece el destinatario.</param>
/// <param name="UserId">El usuario destinatario, si va dirigido a uno.</param>
/// <param name="GroupName">El grupo destinatario sin calificar; el handler le antepone el tenant.</param>
/// <param name="EventName">El nombre que el frontend escucha.</param>
/// <param name="JsonPayload">El contenido, ya serializado en camelCase por el SDK.</param>
public record PushLiveCommand(Guid Tenant, Guid? UserId, string? GroupName, string EventName, string JsonPayload) : IRequest<bool>;

/// <summary>
/// Validador de <see cref="PushLiveCommand"/>.
/// </summary>
public class Validator : AbstractValidator<PushLiveCommand>
{
    /// <summary>Inicializa las reglas de validacion del comando.</summary>
    public Validator()
    {
        RuleFor(x => x.Tenant).NotEmpty();
        RuleFor(x => x.EventName).NotEmpty();
        RuleFor(x => x.JsonPayload).NotEmpty();
        // Uno de los dos, nunca ninguno y nunca los dos: el destino tiene que quedar sin ambiguedad.
        RuleFor(x => x).Must(x => x.UserId.HasValue ^ !string.IsNullOrWhiteSpace(x.GroupName))
            .WithMessage("A live push targets either a user or a group, never both and never neither.");
    }
}
