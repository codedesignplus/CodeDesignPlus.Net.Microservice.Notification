using CodeDesignPlus.Net.Hangfire.Abstractions;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.DeliverPendingNotifications;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Constants;
using CodeDesignPlus.Net.Redis.Abstractions;
using CodeDesignPlus.Net.Security.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Hubs;

[Authorize]
public class MainHub(IUserContext context, IJobService jobService, IRedisFactory redisFactory, ILogger<MainHub> logger) : Hub
{
    /// <summary>Una reentrega por usuario y minuto. El replay es idempotente, pero no es gratis.</summary>
    private static readonly TimeSpan VentanaDeReplay = TimeSpan.FromMinutes(1);

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId} | User Id: {UserId} | Tenant Id: {TenantId}", Context.ConnectionId, context.IdUser, context.Tenant);

        if (context.Tenant != Guid.Empty)
        {
            logger.LogInformation("Adding connection {ConnectionId} to tenant group {TenantGroup}", Context.ConnectionId, context.Tenant);

            await Groups.AddToGroupAsync(Context.ConnectionId, $"{GroupConstants.TenantGroupPrefix}:{context.Tenant}");
        }

        if (context.IdUser != Guid.Empty && await ShouldReplayAsync())
        {
            var command = new DeliverPendingNotificationsCommand(context.Tenant, context.IdUser, Context.ConnectionId);
            var jobId = jobService.Enqueue<IMediator>(mediator => mediator.Send(command, default));

            logger.LogInformation("Enqueued pending notifications delivery command via job {JobId} for user {UserId}, connection {ConnectionId}", jobId, context.IdUser, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Decide si esta conexion merece encolar la reentrega de pendientes.
    /// </summary>
    /// <remarks>
    /// Antes se encolaba un job en **cada** conexion. Con la conexion global y la reconexion automatica
    /// del cliente, eso es un job por cada corte de red de cada usuario: un movil que entra y sale de
    /// cobertura genera decenas por hora, y una caida de red del edificio los genera todos a la vez.
    /// <para>
    /// La marca se pone con <c>StringSetAsync(..., When.NotExists)</c>, que es atomico, y no con un
    /// <c>Exists</c> seguido de un <c>Set</c>: dos reconexiones simultaneas pasarian las dos por el
    /// hueco entre ambas llamadas, que es justo el caso que esto viene a evitar.
    /// </para>
    /// <para>
    /// Si Redis no responde se deja pasar el job. Perder una reentrega es peor que encolar una de mas:
    /// el usuario se quedaria sin ver lo que le llego mientras no estaba.
    /// </para>
    /// </remarks>
    private async Task<bool> ShouldReplayAsync()
    {
        try
        {
            var redis = redisFactory.Create(FactoryConst.RedisCore);

            return await redis.Database.StringSetAsync(
                $"PendingDelivery:{context.Tenant}:{context.IdUser}",
                Context.ConnectionId,
                VentanaDeReplay,
                When.NotExists);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo consultar la marca de reentrega para el usuario {UserId}; se encola de todos modos.", context.IdUser);

            return true;
        }
    }

    public async Task JoinGroup(string groupName)
    {
        var group = GroupConstants.BuildTenantGroupName(context.Tenant, groupName);
        logger.LogWarning("Added connection id {ConnectionId} to Group {Group}", Context.ConnectionId, group);

        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    public async Task LeaveGroup(string groupName)
    {
        var group = GroupConstants.BuildTenantGroupName(context.Tenant, groupName);
        logger.LogWarning("Removed connection id {ConnectionId} to Group {Group}", Context.ConnectionId, group);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }
}