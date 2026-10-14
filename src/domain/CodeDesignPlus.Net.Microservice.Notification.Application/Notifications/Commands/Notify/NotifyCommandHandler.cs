using CodeDesignPlus.Net.Microservice.Notification.Domain.Constants;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Enums;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.Notify;

/// <summary>
/// Manejador de <see cref="NotifyCommand"/>.
/// </summary>
/// <remarks>
/// El orden importa: <b>se guarda pase lo que pase con el push</b>. Si guardar dependiera de empujar, el
/// aviso se perderia justo cuando mas falta hace, que es cuando el destinatario no esta mirando.
/// <para>
/// Y ojo con lo que significa "empujado": <c>Clients.User(id)</c> sin conexiones es un no-op que no lanza,
/// asi que <c>WasSuccess</c> quiere decir "no exploto", no "llego". Quien lo lea despues debe mirar
/// <c>DeliveredAt</c>.
/// </para>
/// </remarks>
public class NotifyCommandHandler(INotifierGateway notifier, INotificationsRepository repository)
    : IRequestHandler<NotifyCommand, bool>
{
    /// <summary>Persiste el aviso y lo empuja segun su audiencia.</summary>
    public async Task<bool> Handle(NotifyCommand request, CancellationToken cancellationToken)
    {
        // Una reentrega del bus llega con el mismo id, porque el emisor lo deriva del evento (pendings/250). Ese aviso
        // ya se guardo y se empujo: repetir el push mostraria el hecho dos veces, y guardarlo de nuevo chocaria con el
        // _id ya guardado.
        if (await repository.ExistsAsync<NotificationsAggregate>(request.Id, request.Tenant, cancellationToken))
            return true;

        var aggregate = NotificationsAggregate.CreateNotice(
            request.Id, request.Audience, request.Kind, request.Title, request.Body,
            request.Resource, request.JsonPayload, request.Tenant, request.SentBy, request.OccurredAt);

        // El cierre va directo y el aviso por una cola: si la tarea ya se hizo despues del hecho que avisa, el aviso nace
        // cerrado y no se empuja, porque ya no hay nada que hacer (pendings/325).
        if (request.Resource is { } resource)
        {
            var resolution = await repository.FindAsync<NotificationResolutionAggregate>(
                NotificationResolutionAggregate.IdFor(request.Tenant, request.Kind, resource.Module, resource.AggregateId), request.Tenant, cancellationToken);

            if (resolution is not null && resolution.Covers(request.OccurredAt))
            {
                aggregate.Resolve(resolution.ResolvedBy, resolution.ResolvedAt);
                aggregate.MarkAsSent(request.SentBy);

                await repository.CreateAsync(aggregate, cancellationToken);

                return true;
            }
        }

        var payload = request.JsonPayload ?? "{}";

        try
        {
            switch (request.Audience.Kind)
            {
                case AudienceKind.User:
                    foreach (var userId in request.Audience.Values)
                        await notifier.SendToUserAsync(Guid.Parse(userId), request.Kind, payload, cancellationToken);
                    break;

                case AudienceKind.Role:
                    foreach (var role in request.Audience.Values)
                        await notifier.SendToGroupAsync(
                            GroupConstants.BuildTenantGroupName(request.Tenant, $"Role:{role}"),
                            request.Kind, payload, cancellationToken);
                    break;

                case AudienceKind.Tenant:
                default:
                    await notifier.BroadcastAsync(request.Tenant, request.Kind, payload, cancellationToken);
                    break;
            }

            aggregate.MarkAsSent(request.SentBy);
        }
        catch (Exception ex)
        {
            aggregate.MarkAsFailed(ex.Message, request.SentBy);
        }

        await repository.CreateAsync(aggregate, cancellationToken);

        return aggregate.WasSuccess;
    }
}
