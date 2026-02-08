using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.BroadcastNotification;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.SendToGroupNotification;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.SendToUserNotification;
using NodaTime;
using NodaTime.Serialization.Protobuf;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Core.Mapster;

public static class MapsterConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<NotificationUserRequest, SendToUserNotificationCommand>
            .NewConfig()
            .ConstructUsing(src => new SendToUserNotificationCommand(
                Guid.Parse(src.Id),
                Guid.Parse(src.UserId),
                src.EventName,
                src.JsonPayload,
                Guid.Parse(src.Tenant),
                string.IsNullOrEmpty(src.SentBy) ? Guid.Empty : Guid.Parse(src.SentBy)
            ));

        TypeAdapterConfig<NotificationBroadcastRequest, BroadcastNotificationCommand>
            .NewConfig()
            .ConstructUsing(src => new BroadcastNotificationCommand(
                Guid.Parse(src.Id),
                src.EventName,
                src.JsonPayload,
                Guid.Parse(src.Tenant),
                string.IsNullOrEmpty(src.SentBy) ? Guid.Empty : Guid.Parse(src.SentBy)
            ));

        TypeAdapterConfig<NotificationGroupRequest, SendToGroupNotificationCommand>
            .NewConfig()
            .ConstructUsing(src => new SendToGroupNotificationCommand(
                Guid.Parse(src.Id),
                src.GroupName,
                src.EventName,
                src.JsonPayload,
                Guid.Parse(src.Tenant),
                string.IsNullOrEmpty(src.SentBy) ? Guid.Empty : Guid.Parse(src.SentBy)
            ));
    }
}