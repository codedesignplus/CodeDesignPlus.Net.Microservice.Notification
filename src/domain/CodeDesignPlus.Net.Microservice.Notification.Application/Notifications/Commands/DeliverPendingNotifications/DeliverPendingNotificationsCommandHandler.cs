using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.MarkAsDelivered;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Repositories;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.DeliverPendingNotifications;

/// <summary>
/// Reentrega a un usuario recien conectado lo que le llego mientras no estaba.
/// </summary>
/// <remarks>
/// Corre en segundo plano por Hangfire para no bloquear la conexion de SignalR, y usa la abstraccion
/// <c>INotificationDeliveryService</c> para no arrastrar SignalR a la capa de aplicacion.
/// <para>
/// Devuelve lo que <b>de verdad</b> paso. Antes se tragaba cada fallo en un catch y luego registraba
/// "Successfully delivered {Count}" con el total de pendientes: si fallaban los doce, el log decia que
/// se entregaron doce. Es el defecto contra el que se escribio la regla 23.
/// </para>
/// </remarks>
public class DeliverPendingNotificationsCommandHandler(
    IMediator mediator,
    INotificationDeliveryService deliveryService,
    ILogger<DeliverPendingNotificationsCommandHandler> logger,
    INotificationsRepository repository) : IRequestHandler<DeliverPendingNotificationsCommand, DeliverPendingResult>
{
    /// <summary>Reentrega los pendientes y deja constancia de cuantos llegaron y cuantos no.</summary>
    public async Task<DeliverPendingResult> Handle(DeliverPendingNotificationsCommand request, CancellationToken cancellationToken)
    {
        var pending = await repository.GetPendingByUserIdAsync(request.Tenant, request.UserId, cancellationToken);

        if (pending is null || pending.Count == 0)
        {
            logger.LogInformation("Sin pendientes para el usuario {UserId} en la copropiedad {Tenant}.", request.UserId, request.Tenant);

            return new DeliverPendingResult(0, 0);
        }

        var entregados = 0;
        var fallidos = 0;

        foreach (var notification in pending)
        {
            try
            {
                await deliveryService.DeliverToConnectionAsync(
                    request.ConnectionId,
                    notification.Id,
                    notification.EventName!,
                    notification.PayloadPreview,
                    cancellationToken);

                await mediator.Send(new MarkNotificationAsDeliveredCommand(notification.Id, request.UserId), cancellationToken);

                entregados++;
            }
            catch (Exception ex)
            {
                fallidos++;

                logger.LogError(ex, "No se pudo reentregar el aviso {NotificationId} al usuario {UserId}.", notification.Id, request.UserId);
            }
        }

        logger.LogInformation(
            "Reentrega para el usuario {UserId}: {Delivered} entregados, {Failed} fallidos de {Total}.",
            request.UserId, entregados, fallidos, pending.Count);

        // Un job que no entrego nada no puede terminar en verde. Si algo llego, el fallo parcial queda en
        // el log y en el resultado, pero no tumba la reentrega de los demas.
        if (entregados == 0 && fallidos > 0)
            throw new InvalidOperationException($"No se pudo reentregar ninguno de los {fallidos} avisos pendientes del usuario {request.UserId}.");

        return new DeliverPendingResult(entregados, fallidos);
    }
}
