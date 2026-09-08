using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;
using CodeDesignPlus.Net.Microservice.Notification.gRpc.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Services;

/// <summary>
/// SignalR implementation of INotificationDeliveryService.
/// Delivers notifications to specific connections via SignalR hub.
/// </summary>
public class SignalRNotificationDeliveryService(IHubContext<MainHub> hubContext) : INotificationDeliveryService
{
    /// <summary>Reenvia un aviso pendiente a una conexion concreta.</summary>
    /// <remarks>
    /// El mismo sobre que la entrega en vivo: el JSON crudo, y nada mas.
    /// <para>
    /// Antes mandaba <c>{ notificationId, eventName, payload }</c>. El frontend busca <c>jsonPayload</c>,
    /// no lo encontraba, y entregaba el sobre entero como si fuera el payload: todos los campos del
    /// aviso llegaban <c>undefined</c>, sin un solo error en ninguna de las dos puntas.
    /// </para>
    /// </remarks>
    public Task DeliverToConnectionAsync(
        string connectionId,
        Guid notificationId,
        string eventName,
        string? payloadJson,
        CancellationToken cancellationToken)
        => hubContext.Clients.Client(connectionId).SendAsync(eventName, payloadJson ?? "{}", cancellationToken);
}
