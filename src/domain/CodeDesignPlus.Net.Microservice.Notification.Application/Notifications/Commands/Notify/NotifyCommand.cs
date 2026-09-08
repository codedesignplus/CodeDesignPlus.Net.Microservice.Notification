using CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.Notify;

/// <summary>
/// Guarda un aviso durable y, si hay alguien conectado, lo empuja tambien.
/// </summary>
/// <remarks>
/// La pregunta que decide entre esto y <c>PushLiveCommand</c>, por cada punto de llamada:
/// <b>le sirve a alguien que no estaba mirando la pantalla?</b> Si -> aqui. No -> push efimero.
/// </remarks>
/// <param name="Id">Generado por el emisor: es la clave de idempotencia frente a reentregas del bus.</param>
/// <param name="OccurredAt">Cuando ocurrio el hecho, que no es cuando se guarda el aviso.</param>
public record NotifyCommand(
    Guid Id, Guid Tenant, Audience Audience, string Kind, string Title, string Body,
    ResourceRef? Resource, string? JsonPayload, Guid SentBy, Instant OccurredAt) : IRequest<bool>;

/// <summary>
/// Validador de <see cref="NotifyCommand"/>.
/// </summary>
public class Validator : AbstractValidator<NotifyCommand>
{
    /// <summary>Inicializa las reglas de validacion del comando.</summary>
    public Validator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Tenant).NotEmpty();
        RuleFor(x => x.Audience).NotNull();
        RuleFor(x => x.Kind).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();
    }
}
