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
    /// <param name="openOnly">Solo los avisos sin cerrar: con "solo sin leer", uno cerrado ya no espera nada del lector.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>La pagina pedida y cuantos avisos le alcanzan en total.</returns>
    Task<Pagination<NotificationsAggregate>> GetInboxAsync(Guid tenant, Guid userId, string[] roles, C.Criteria criteria, IReadOnlyCollection<Guid>? excluir, bool openOnly, CancellationToken cancellationToken);

    /// <summary>
    /// Cuantos avisos le alcanzan sin leer y sin cerrar, para el contador de la campana.
    /// </summary>
    /// <param name="tenant">La copropiedad del lector.</param>
    /// <param name="userId">El lector.</param>
    /// <param name="roles">Los roles del lector en esa copropiedad.</param>
    /// <param name="leidos">Los avisos que el lector ya leyo.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Los pendientes de la campana.</returns>
    Task<long> CountPendingAsync(Guid tenant, Guid userId, string[] roles, IReadOnlyCollection<Guid> leidos, CancellationToken cancellationToken);

    /// <summary>
    /// Cierra los avisos abiertos de esos tipos que llevan a ese recurso, de hechos anteriores o iguales al cierre.
    /// </summary>
    /// <param name="tenant">La copropiedad.</param>
    /// <param name="kinds">Los tipos de aviso.</param>
    /// <param name="module">El modulo del recurso.</param>
    /// <param name="aggregateId">El recurso.</param>
    /// <param name="resolvedBy">Quien hizo la tarea.</param>
    /// <param name="resolvedAt">Cuando.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Cuantos avisos se cerraron.</returns>
    Task<long> ResolveAsync(Guid tenant, IReadOnlyCollection<string> kinds, string module, string aggregateId, Guid resolvedBy, Instant resolvedAt, CancellationToken cancellationToken);
}