using System;
using CodeDesignPlus.Net.Security.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Hubs;

[Authorize]
public class MainHub(IUserContext context, ILogger<MainHub> logger) : Hub
{
    private const string TenantGroupPrefix = "Tenant";
    public override async Task OnConnectedAsync()
    {
        logger.LogWarning("Client connected: {ConnectionId}", Context.ConnectionId);

        //Write logger user
        logger.LogWarning("User connected: {UserId}", context.IdUser);
        logger.LogWarning("Tenant connected: {TenantId}", context.Tenant);

        if (context.Tenant != Guid.Empty)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"{TenantGroupPrefix}:{context.Tenant}");

        await base.OnConnectedAsync();
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{TenantGroupPrefix}:{context.Tenant}:{groupName}");
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{TenantGroupPrefix}:{context.Tenant}:{groupName}");
    }
}