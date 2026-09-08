using System;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Constants;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;
using Microsoft.AspNetCore.SignalR;

namespace CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Services;

public class SignalRNotifierAdapter<THub>(IHubContext<THub> hubContext) 
    : INotifierGateway where THub : Hub
{
    public async Task SendToUserAsync(Guid userId, string method, string payload, CancellationToken ct)
    {
        await hubContext.Clients.User(userId.ToString()).SendAsync(method, payload, cancellationToken: ct);
    }

    /// <summary>
    /// Emite al grupo de la copropiedad, no a todo el mundo.
    /// </summary>
    /// <remarks>
    /// <c>MainHub</c> ya mete cada conexion en <c>Tenant:{tenant}</c> al conectar; ese grupo existia y no se
    /// usaba como destino nunca. Con <c>Clients.All</c>, cada pqrs, cada factura y cada votacion de asamblea
    /// llegaba a los navegadores de todas las copropiedades.
    /// </remarks>
    public async Task BroadcastAsync(Guid tenant, string method, string payload, CancellationToken ct)
    {
        var group = $"{GroupConstants.TenantGroupPrefix}:{tenant}";

        await hubContext.Clients.Group(group).SendAsync(method, payload, cancellationToken: ct);
    }

    public async Task SendToGroupAsync(string group, string method, string payload, CancellationToken ct)
    {
        await hubContext.Clients.Group(group).SendAsync(method, payload, cancellationToken: ct);
    }
}