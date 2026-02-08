using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.BroadcastNotification;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.SendToGroupNotification;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.SendToUserNotification;
using CodeDesignPlus.Net.Microservice.Notification.gRpc.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Services;

public class NotificationsService(IMediator mediator, IMapper mapper) : Notifier.NotifierBase
{
    public override async Task Broadcast(
        IAsyncStreamReader<NotificationBroadcastRequest> requestStream,
        IServerStreamWriter<NotificationResponse> responseStream,
        ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            try
            {
                var command = mapper.Map<BroadcastNotificationCommand>(request);

                var result = await mediator.Send(command, context.CancellationToken);

                await responseStream.WriteAsync(new NotificationResponse
                {
                    Success = true,
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
                var command = mapper.Map<SendToUserNotificationCommand>(request);
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
                var command = mapper.Map<SendToGroupNotificationCommand>(request);
                await mediator.Send(command, context.CancellationToken);

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