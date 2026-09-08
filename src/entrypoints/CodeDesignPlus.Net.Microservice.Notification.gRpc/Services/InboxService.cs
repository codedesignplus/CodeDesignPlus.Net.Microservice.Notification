using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.Notify;
using Google.Protobuf.WellKnownTypes;
using NodaTime.Extensions;

// El proto genera Audience en este mismo namespace, asi que el VO del dominio se alias para que quede
// visible cual es cual en cada linea de la traduccion.
using DomainAudience = CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects.Audience;
using DomainResource = CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects.ResourceRef;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Services;

/// <summary>
/// Punto de entrada de los avisos durables.
/// </summary>
/// <remarks>
/// Todo lo que entra por aqui se guarda, pase lo que pase con el push. Es lo que hace que la bandeja
/// tenga contenido y que un flujo no se pierda porque el residente reinicio el navegador.
/// </remarks>
public class InboxService(IMediator mediator) : Inbox.InboxBase
{
    /// <summary>Persiste el aviso y lo empuja a su audiencia.</summary>
    public override async Task Notify(IAsyncStreamReader<NotificationRequest> requestStream, IServerStreamWriter<NotifyAck> responseStream, ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            // El id se saca fuera del try: un acuse sin el no le sirve al emisor para correlacionar.
            var notificationId = request.Id;

            try
            {
                var command = new NotifyCommand(
                    ParseGuid(notificationId, nameof(request.Id)),
                    ParseGuid(request.Tenant, nameof(request.Tenant)),
                    ToAudience(request.Audience),
                    request.Kind,
                    request.Title,
                    request.Body,
                    ToResource(request.Resource),
                    request.JsonPayload,
                    ParseGuid(request.SentBy, nameof(request.SentBy)),
                    ToInstant(request.OccurredAt));

                var result = await mediator.Send(command, context.CancellationToken);

                await responseStream.WriteAsync(new NotifyAck
                {
                    Success = result,
                    // Guardado si, entregado no necesariamente: el handler no puede saber si habia alguien
                    // conectado, y por eso existe la bandeja.
                    Message = result ? "Stored" : "Stored, not delivered",
                    NotificationId = notificationId
                });
            }
            catch (Exception ex)
            {
                await responseStream.WriteAsync(new NotifyAck { Success = false, Message = ex.Message, NotificationId = notificationId });
            }
        }
    }

    /// <summary>Traduce el descriptor del contrato al del dominio.</summary>
    /// <remarks>
    /// Un <c>UNSPECIFIED</c> se rechaza en vez de tomar un valor por defecto: caer en "toda la copropiedad"
    /// por omision mandaria un aviso privado a todo el conjunto.
    /// </remarks>
    private static DomainAudience ToAudience(Audience? audience) => audience?.Kind switch
    {
        AudienceKind.User => DomainAudience.ForUsers(audience.Values.Select(x => ParseGuid(x, "audience.values"))),
        AudienceKind.Role => DomainAudience.ForRoles(audience.Values),
        AudienceKind.Tenant => DomainAudience.ForTenant(),
        _ => throw new InvalidCastException("A notification requires an audience kind: user, role or tenant.")
    };

    private static DomainResource? ToResource(Resource? resource)
        => resource is null || string.IsNullOrWhiteSpace(resource.Module) ? null : DomainResource.Create(resource.Module, resource.AggregateId);

    /// <summary>Cuando ocurrio el hecho; si el emisor no lo mando, ahora.</summary>
    private static Instant ToInstant(Timestamp? occurredAt)
        => occurredAt is null || occurredAt.Seconds == 0 ? SystemClock.Instance.GetCurrentInstant() : occurredAt.ToDateTimeOffset().ToInstant();

    private static Guid ParseGuid(string value, string field)
        => Guid.TryParse(value, out var parsed) ? parsed : throw new InvalidCastException($"Invalid {field} format: {value}");
}
