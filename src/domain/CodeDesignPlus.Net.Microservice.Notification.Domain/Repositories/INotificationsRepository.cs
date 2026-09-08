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
    Task<List<NotificationsAggregate>> GetPendingByUserIdAsync(Guid tenant, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Marca una notificación como entregada.
    /// </summary>
    /// <param name="notificationId">ID de la notificación</param>
    /// <param name="deliveredAt">Timestamp de entrega</param>
    /// <param name="updateBy">Usuario que actualiza</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task MarkAsDeliveredAsync(Guid notificationId, Instant deliveredAt, Guid updateBy, CancellationToken cancellationToken);

    /// <summary>
    /// La bandeja de un lector: los avisos dirigidos a el, a un rol que tiene, o a toda su copropiedad.
    /// </summary>
    /// <remarks>
    /// La audiencia se guardo como descriptor al escribir y se resuelve aqui, al leer. Al leer sale
    /// gratis: los roles los trae el propio lector en su JWT.
    /// </remarks>
    /// <param name="tenant">La copropiedad del lector.</param>
    /// <param name="userId">El lector.</param>
    /// <param name="roles">Los roles que el lector trae en su token.</param>
    /// <param name="page">Pagina, empezando en cero.</param>
    /// <param name="size">Tamaño de pagina.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Los avisos que le alcanzan, del mas reciente al mas antiguo.</returns>
    Task<List<NotificationsAggregate>> GetInboxAsync(Guid tenant, Guid userId, string[] roles, int page, int size, CancellationToken cancellationToken);

    /// <summary>Cuantos avisos le alcanzan, para el contador de la campana.</summary>
    Task<long> CountInboxAsync(Guid tenant, Guid userId, string[] roles, CancellationToken cancellationToken);
}