namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetAllNotifications;

public record GetAllNotificationsQuery(Guid Id) : IRequest<NotificationsDto>;

