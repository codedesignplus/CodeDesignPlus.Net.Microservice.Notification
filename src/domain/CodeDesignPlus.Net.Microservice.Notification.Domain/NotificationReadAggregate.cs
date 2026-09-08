namespace CodeDesignPlus.Net.Microservice.Notification.Domain;

/// <summary>
/// El acuse de que un usuario leyo un aviso.
/// </summary>
/// <remarks>
/// Vive en su propia coleccion y no como un array dentro del aviso: un aviso dirigido a toda la
/// copropiedad lo leerian <b>todos</b> sus usuarios, y ese array no tendria techo. Un documento que crece
/// sin limite acaba pasandose del tamaño maximo de Mongo, y lo hace el dia que el conjunto es grande.
/// <para>
/// El identificador es determinista, derivado del par (aviso, lector), para que marcar leido sea
/// idempotente por construccion: un doble clic, o la misma pantalla abierta en dos pestañas, actualizan
/// la misma fila.
/// </para>
/// </remarks>
public class NotificationReadAggregate(Guid id) : AggregateRootBase(id)
{
    /// <summary>El aviso que se leyo.</summary>
    public Guid NotificationId { get; private set; }

    /// <summary>Quien lo leyo.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Cuando lo leyo.</summary>
    public Instant ReadAt { get; private set; }

    /// <summary>La copropiedad, para que el acuse no cruce entre conjuntos.</summary>
    public Guid Tenant { get; private set; }

    /// <summary>Registra que un usuario leyo un aviso.</summary>
    /// <param name="notificationId">El aviso leido.</param>
    /// <param name="userId">El lector.</param>
    /// <param name="tenant">La copropiedad, para que el acuse no cruce entre conjuntos.</param>
    public static NotificationReadAggregate Create(Guid notificationId, Guid userId, Guid tenant)
    {
        DomainGuard.GuidIsEmpty(notificationId, Errors.NotificationIdIsRequired);
        DomainGuard.GuidIsEmpty(userId, Errors.ReaderIsRequired);

        var ahora = SystemClock.Instance.GetCurrentInstant();

        return new NotificationReadAggregate(DeterministicGuid.From($"{notificationId}:{userId}"))
        {
            NotificationId = notificationId,
            UserId = userId,
            Tenant = tenant,
            ReadAt = ahora,
            CreatedAt = ahora,
            CreatedBy = userId,
            IsActive = true
        };
    }
}
