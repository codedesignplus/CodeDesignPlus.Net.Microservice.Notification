using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Hubs;

//[Authorize] 
public class MainHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Opcional: Lógica al conectar, ej: loguear el Context.UserIdentifier
        await base.OnConnectedAsync();
    }

    // Método para que el frontend se una a grupos manualmente si es necesario
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}