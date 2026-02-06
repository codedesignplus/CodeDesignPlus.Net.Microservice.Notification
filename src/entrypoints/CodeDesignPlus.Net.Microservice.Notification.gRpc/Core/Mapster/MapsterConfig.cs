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
        TypeAdapterConfig<NotificationRequest, SendToUserNotificationCommand>
            .NewConfig()
            .Map(dest => dest.Id, src => Guid.NewGuid()) // Generamos ID del comando
            .Map(dest => dest.TraceId, src => Guid.NewGuid().ToString()) // Generamos o extraemos TraceId
            .Map(dest => dest.UserId, src => src.TargetId)
            .Map(dest => dest.MethodName, src => src.MethodName)
            .Map(dest => dest.JsonPayload, src => src.JsonPayload);

        TypeAdapterConfig<NotificationRequest, BroadcastNotificationCommand>
            .NewConfig()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.TraceId, src => Guid.NewGuid().ToString())
            .Map(dest => dest.MethodName, src => src.MethodName)
            .Map(dest => dest.JsonPayload, src => src.JsonPayload);

        TypeAdapterConfig<NotificationGroupRequest, SendToGroupNotificationCommand>
            .NewConfig()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.TraceId, src => Guid.NewGuid().ToString())
            .Map(dest => dest.GroupName, src => src.GroupName)
            .Map(dest => dest.MethodName, src => src.MethodName)
            .Map(dest => dest.JsonPayload, src => src.JsonPayload);
    }
}