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
    /// <para>
    /// El filtro de audiencia y el de <paramref name="criteria"/> se combinan con <c>AND</c>, nunca se
    /// sustituyen: quien llama puede acotar la bandeja, no ensancharla, asi que ningun <c>filters</c> de
    /// la URL puede sacar un aviso que no le corresponde.
    /// </para>
    /// </remarks>
    /// <param name="tenant">La copropiedad del lector.</param>
    /// <param name="userId">El lector.</param>
    /// <param name="roles">Los roles que el lector trae en su token.</param>
    /// <param name="criteria">Filtros, orden y pagina que pide quien consulta.</param>
    /// <param name="excluir">Avisos que no deben salir, para el filtro de "solo sin leer".</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La pagina pedida y cuantos avisos le alcanzan en total.</returns>
    Task<Pagination<NotificationsAggregate>> GetInboxAsync(Guid tenant, Guid userId, string[] roles, C.Criteria criteria, IReadOnlyCollection<Guid>? excluir, CancellationToken cancellationToken);

    /// <summary>Cuantos avisos le alcanzan, para el contador de la campana.</summary>
    Task<long> CountInboxAsync(Guid tenant, Guid userId, string[] roles, CancellationToken cancellationToken);
}