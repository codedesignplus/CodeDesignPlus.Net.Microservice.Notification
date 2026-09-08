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
        var aggregate = NotificationsAggregate.CreateNotice(
            request.Id, request.Audience, request.Kind, request.Title, request.Body,
            request.Resource, request.JsonPayload, request.Tenant, request.SentBy, request.OccurredAt);

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
