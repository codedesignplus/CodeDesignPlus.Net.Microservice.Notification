using System;

namespace CodeDesignPlus.Net.Microservice.Notification.Domain.Services;

public interface INotifierGateway
{
    Task SendToUserAsync(Guid userId, string method, string payload, CancellationToken cancellationToken);
    /// <summary>
    /// Emite a todas las conexiones de una copropiedad.
    /// </summary>
    /// <remarks>
    /// El tenant es obligatorio a proposito: la version anterior usaba <c>Clients.All</c> y sacaba cada
    /// aviso fuera de la copropiedad que lo origino. Hacerlo parametro obliga a decir a quien va.
    /// </remarks>
    Task BroadcastAsync(Guid tenant, string method, string payload, CancellationToken cancellationToken);
    Task SendToGroupAsync(string group, string method, string payload, CancellationToken cancellationToken);
}