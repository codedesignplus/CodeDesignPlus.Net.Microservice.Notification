using CodeDesignPlus.Net.Hangfire.Abstractions;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.DeliverPendingNotifications;
using CodeDesignPlus.Net.Security.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Hubs;

[Authorize]
public class MainHub(IUserContext context, IJobService jobService, ILogger<MainHub> logger) : Hub
{
    private const string TenantGroupPrefix = "Tenant";

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId} | User Id: {UserId} | Tenant Id: {TenantId}", Context.ConnectionId, context.IdUser, context.Tenant);

        if (context.Tenant != Guid.Empty)
        {
            logger.LogInformation("Adding connection {ConnectionId} to tenant group {TenantGroup}", Context.ConnectionId, context.Tenant);

            await Groups.AddToGroupAsync(Context.ConnectionId, $"{TenantGroupPrefix}:{context.Tenant}");
        }

        if (context.IdUser != Guid.Empty)
        {
            var command = new DeliverPendingNotificationsCommand(context.IdUser, Context.ConnectionId);
            var jobId = jobService.Enqueue<IMediator>(mediator => mediator.Send(command, default));

            logger.LogInformation("Enqueued pending notifications delivery command via job {JobId} for user {UserId}, connection {ConnectionId}", jobId, context.IdUser, Context.ConnectionId);
        }

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