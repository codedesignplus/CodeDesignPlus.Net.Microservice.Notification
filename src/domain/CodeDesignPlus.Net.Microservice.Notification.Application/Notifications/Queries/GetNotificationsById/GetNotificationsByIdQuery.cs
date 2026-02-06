namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetNotificationsById;

public record GetNotificationsByIdQuery(Guid Id) : IRequest<NotificationsDto>;

