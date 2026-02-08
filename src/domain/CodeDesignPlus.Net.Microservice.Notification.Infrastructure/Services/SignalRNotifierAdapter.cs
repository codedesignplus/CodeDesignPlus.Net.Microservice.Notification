using System;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;
using Microsoft.AspNetCore.SignalR;

namespace CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Services;

public class SignalRNotifierAdapter<THub>(IHubContext<THub> hubContext) 
    : INotifierGateway where THub : Hub
{
    public async Task SendToUserAsync(Guid userId, string method, string payload, CancellationToken ct)
    {
        // TODO: Asegurar que el 'userId' coincide con el Claim 'sub' o UserIdentifier del JWT del frontend
        await hubContext.Clients.User(userId.ToString()).SendAsync(method, payload, cancellationToken: ct);
    }

    public async Task BroadcastAsync(string method, string payload, CancellationToken ct)
    {
        await hubContext.Clients.All.SendAsync(method, payload, cancellationToken: ct);
    }

    public async Task SendToGroupAsync(string group, string method, string payload, CancellationToken ct)
    {
        await hubContext.Clients.Group(group).SendAsync(method, payload, cancellationToken: ct);
    }
}