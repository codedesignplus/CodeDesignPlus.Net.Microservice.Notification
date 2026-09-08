namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetUnreadCount;

/// <summary>
/// Cuantos avisos sin leer tiene el lector, para el numerito de la campana.
/// </summary>
public record GetUnreadCountQuery : IRequest<long>;
