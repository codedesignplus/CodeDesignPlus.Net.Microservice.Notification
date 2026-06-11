namespace CodeDesignPlus.Net.Microservice.Notification.Domain.Repositories;

public interface INotificationsRepository : IRepositoryBase
{
    /// <summary>
    /// Obtiene todas las notificaciones pendientes (no entregadas) de un usuario.
    /// Filtra SOLO notificaciones Type=User.
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de notificaciones con DeliveredAt == null y Type == User</returns>
    Task<List<NotificationsAggregate>> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Marca una notificación como entregada.
    /// </summary>
    /// <param name="notificationId">ID de la notificación</param>
    /// <param name="deliveredAt">Timestamp de entrega</param>
    /// <param name="updateBy">Usuario que actualiza</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task MarkAsDeliveredAsync(Guid notificationId, Instant deliveredAt, Guid updateBy, CancellationToken cancellationToken);
}