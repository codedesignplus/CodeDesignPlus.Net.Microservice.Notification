namespace CodeDesignPlus.Net.Microservice.Notification.Domain.Repositories;

/// <summary>
/// Los acuses de lectura, en su propia coleccion.
/// </summary>
public interface INotificationReadRepository : IRepositoryBase
{
    /// <summary>
    /// Registra que un usuario leyo un aviso.
    /// </summary>
    /// <remarks>
    /// Es un upsert sobre el identificador determinista del acuse, no un insert: marcar leido dos veces
    /// no puede fallar ni duplicar.
    /// </remarks>
    Task MarkAsReadAsync(NotificationReadAggregate receipt, CancellationToken cancellationToken);

    /// <summary>
    /// Cuales de esos avisos ya leyo el usuario.
    /// </summary>
    /// <remarks>
    /// Se pregunta por los identificadores de la pagina que se acaba de traer, no por todos los del
    /// usuario: una bandeja de dos años no cabe en memoria y no hace falta para pintar veinte filas.
    /// </remarks>
    Task<HashSet<Guid>> GetReadIdsAsync(Guid tenant, Guid userId, IEnumerable<Guid> notificationIds, CancellationToken cancellationToken);

    /// <summary>
    /// Todos los avisos que el usuario ya leyo en esa copropiedad.
    /// </summary>
    /// <remarks>
    /// Solo se pide cuando el filtro es "solo sin leer": ahi el descarte tiene que ocurrir <b>antes</b>
    /// de paginar, o el total que se le muestra al usuario cuenta filas que no va a ver y la ultima
    /// pagina sale corta. Es lo contrario de <see cref="GetReadIdsAsync"/>, que pinta el estado de una
    /// pagina ya traida.
    /// <para>
    /// Crece con lo que el usuario haya leido, no con la bandeja entera, y va acotado a una sola
    /// copropiedad.
    /// </para>
    /// </remarks>
    Task<List<Guid>> GetAllReadIdsAsync(Guid tenant, Guid userId, CancellationToken cancellationToken);

    /// <summary>Cuantos de esos avisos <b>no</b> ha leido el usuario.</summary>
    Task<long> CountReadAsync(Guid tenant, Guid userId, CancellationToken cancellationToken);
}
