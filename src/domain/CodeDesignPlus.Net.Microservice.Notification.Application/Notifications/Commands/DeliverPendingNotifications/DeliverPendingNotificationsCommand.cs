namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.DeliverPendingNotifications;

/// <summary>
/// Command to deliver pending notifications (Type=User) to a connected user.
/// Executed in background via Hangfire when a user connects to SignalR.
/// </summary>
/// <param name="Tenant">La copropiedad de la conexion. Sin el, un usuario que pertenece a dos recibe al conectarse los pendientes de ambas.</param>
/// <param name="UserId">ID of the user who connected</param>
/// <param name="ConnectionId">SignalR connection ID</param>
public record DeliverPendingNotificationsCommand(Guid Tenant, Guid UserId, string ConnectionId) : IRequest<DeliverPendingResult>;

/// <summary>
/// Lo que de verdad paso al reentregar.
/// </summary>
/// <remarks>
/// Existe para que el resultado no sea un <c>Unit</c> que nadie puede contradecir: la regla 23 nacio de
/// un job que decia "Successfully delivered 12" aunque fallaran las doce.
/// </remarks>
/// <param name="Delivered">Cuantos llegaron.</param>
/// <param name="Failed">Cuantos no.</param>
public record DeliverPendingResult(int Delivered, int Failed);
