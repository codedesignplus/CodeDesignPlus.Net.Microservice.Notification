namespace CodeDesignPlus.Net.Microservice.Notification.Domain;

/// <summary>
/// La constancia de que una tarea ya se hizo: cierra los avisos de un tipo que llevan a un recurso.
/// </summary>
/// <remarks>
/// <para>
/// El cierre se guarda aparte, y no solo sobre los avisos, porque puede llegar antes que el aviso que cierra: el emisor
/// manda el aviso por una cola y el cierre directo. Un aviso que llega despues de su cierre, por un hecho anterior a el,
/// nace cerrado.
/// </para>
/// <para>
/// El servicio no conoce las tareas de ningun producto. La clave es la misma con la que se mando el aviso —tipo, modulo y
/// agregado— y el identificador se deriva de ella, asi que cerrar dos veces actualiza la misma fila.
/// </para>
/// </remarks>
public class NotificationResolutionAggregate(Guid id) : AggregateRootBase(id)
{
    /// <summary>La copropiedad.</summary>
    public Guid Tenant { get; private set; }

    /// <summary>El tipo de aviso que se cierra.</summary>
    public string Kind { get; private set; } = null!;

    /// <summary>El modulo dueño del recurso.</summary>
    public string Module { get; private set; } = null!;

    /// <summary>El identificador del recurso dentro de ese modulo.</summary>
    public string AggregateId { get; private set; } = null!;

    /// <summary>Cuando se hizo la tarea por ultima vez: los avisos de hechos anteriores quedan cerrados.</summary>
    public Instant ResolvedAt { get; private set; }

    /// <summary>Quien la hizo.</summary>
    public Guid ResolvedBy { get; private set; }

    /// <summary>
    /// El identificador del cierre de esa clave.
    /// </summary>
    /// <param name="tenant">La copropiedad.</param>
    /// <param name="kind">El tipo de aviso.</param>
    /// <param name="module">El modulo.</param>
    /// <param name="aggregateId">El recurso.</param>
    /// <returns>Siempre el mismo para la misma clave.</returns>
    public static Guid IdFor(Guid tenant, string kind, string module, string aggregateId)
        => DeterministicGuid.From($"resolution:{tenant}:{kind}:{module}:{aggregateId}");

    /// <summary>
    /// Registra el cierre de una clave.
    /// </summary>
    /// <param name="tenant">La copropiedad.</param>
    /// <param name="kind">El tipo de aviso.</param>
    /// <param name="module">El modulo.</param>
    /// <param name="aggregateId">El recurso.</param>
    /// <param name="resolvedBy">Quien hizo la tarea.</param>
    /// <param name="resolvedAt">Cuando.</param>
    /// <returns>El cierre.</returns>
    public static NotificationResolutionAggregate Create(Guid tenant, string kind, string module, string aggregateId, Guid resolvedBy, Instant resolvedAt)
    {
        DomainGuard.GuidIsEmpty(tenant, Errors.TenantIsRequired);
        DomainGuard.IsNullOrEmpty(kind, Errors.NotificationKindIsRequired);
        DomainGuard.IsNullOrEmpty(module, Errors.ResourceModuleIsRequired);
        DomainGuard.IsNullOrEmpty(aggregateId, Errors.ResourceAggregateIdIsRequired);

        return new NotificationResolutionAggregate(IdFor(tenant, kind, module, aggregateId))
        {
            Tenant = tenant,
            Kind = kind,
            Module = module,
            AggregateId = aggregateId,
            ResolvedAt = resolvedAt,
            ResolvedBy = resolvedBy,
            IsActive = true,
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
            CreatedBy = resolvedBy
        };
    }

    /// <summary>
    /// Mueve el cierre a una tarea posterior de la misma clave.
    /// </summary>
    /// <param name="resolvedBy">Quien la hizo.</param>
    /// <param name="resolvedAt">Cuando.</param>
    /// <returns><c>false</c> si el cierre que llega no es posterior al que hay: no hay nada que guardar.</returns>
    public bool Advance(Guid resolvedBy, Instant resolvedAt)
    {
        if (resolvedAt <= ResolvedAt)
        {
            return false;
        }

        ResolvedAt = resolvedAt;
        ResolvedBy = resolvedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedBy = resolvedBy;

        return true;
    }

    /// <summary>
    /// Si un aviso de un hecho ocurrido en ese instante ya nace cerrado.
    /// </summary>
    /// <param name="occurredAt">Cuando ocurrio el hecho del aviso.</param>
    /// <returns><c>true</c> si el hecho es anterior o igual al cierre.</returns>
    public bool Covers(Instant occurredAt) => occurredAt <= ResolvedAt;
}
