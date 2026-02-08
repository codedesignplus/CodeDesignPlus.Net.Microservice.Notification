using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.BroadcastNotification;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.SendToGroupNotification;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.SendToUserNotification;
using CodeDesignPlus.Net.Microservice.Notification.gRpc.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Services;

public class NotificationsService(IMediator mediator, ILogger<NotificationsService> logger) : Notifier.NotifierBase
{
    public override async Task Broadcast(IAsyncStreamReader<NotificationBroadcastRequest> requestStream, IServerStreamWriter<NotificationResponse> responseStream, ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {

            try
            {
                if (!Guid.TryParse(request.Id, out var id))
                    throw new InvalidCastException("Invalid Id format" + request.Id);

                if (!Guid.TryParse(request.Tenant, out var tenantId))
                    throw new InvalidCastException("Invalid Tenant format" + request.Tenant);

                if (!Guid.TryParse(request.SentBy, out var sentBy))
                    throw new InvalidCastException("Invalid SentBy format" + request.SentBy);

                var command = new BroadcastNotificationCommand(id, request.EventName, request.JsonPayload, tenantId, sentBy);

                logger.LogInformation("Broadcasting notification with {@Command}", command);

                var result = await mediator.Send(command, context.CancellationToken);

                await responseStream.WriteAsync(new NotificationResponse
                {
                    Success = result,
                    Message = "Broadcast processed"
                });

            }
            catch (Exception ex)
            {
                await responseStream.WriteAsync(new NotificationResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
    }

    public override async Task SendToUser(
        IAsyncStreamReader<NotificationUserRequest> requestStream,
        IServerStreamWriter<NotificationResponse> responseStream,
        ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            try
            {
                var command = new SendToUserNotificationCommand(Guid.Parse(request.Id), Guid.Parse(request.UserId), request.EventName, request.JsonPayload, Guid.Parse(request.Tenant), Guid.Parse(request.SentBy));
                await mediator.Send(command, context.CancellationToken);
                logger.LogInformation("Sending notification to user with {@Command}", command);

                await mediator.Send(command, context.CancellationToken);

                await responseStream.WriteAsync(new NotificationResponse
                {
                    Success = true,
                    Message = $"Sent to {request.UserId}"
                });
            }
            catch (Exception ex)
            {
                await responseStream.WriteAsync(new NotificationResponse { Success = false, Message = ex.Message });
            }
        }
    }

    public override async Task SendToGroup(
        IAsyncStreamReader<NotificationGroupRequest> requestStream,
        IServerStreamWriter<NotificationResponse> responseStream,
        ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            try
            {
                var command = new SendToGroupNotificationCommand(Guid.Parse(request.Id), request.GroupName, request.EventName, request.JsonPayload, Guid.Parse(request.Tenant), Guid.Parse(request.SentBy));

                logger.LogInformation("Sending notification to group with {@Command}", command);

                await responseStream.WriteAsync(new NotificationResponse
                {
                    Success = true,
                    Message = $"Sent to group {request.GroupName}"
                });
            }
            catch (Exception ex)
            {
                await responseStream.WriteAsync(new NotificationResponse { Success = false, Message = ex.Message });
            }
        }
    }
}