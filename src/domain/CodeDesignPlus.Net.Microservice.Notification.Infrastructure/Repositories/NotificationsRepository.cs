namespace CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Repositories;

public class NotificationsRepository(IServiceProvider serviceProvider, IOptions<MongoOptions> mongoOptions, ILogger<NotificationsRepository> logger)
    : RepositoryBase(serviceProvider, mongoOptions, logger), INotificationsRepository
{
    public async Task<List<NotificationsAggregate>> GetPendingByUserIdAsync(Guid tenant, Guid userId, CancellationToken cancellationToken)
    {
        var filter = Builders<NotificationsAggregate>.Filter.And(
            // Sin el tenant, un usuario que pertenece a dos copropiedades recibe al conectarse los
            // pendientes de las dos.
            Builders<NotificationsAggregate>.Filter.Eq(x => x.Tenant, tenant),
            Builders<NotificationsAggregate>.Filter.Eq(x => x.UserId, userId),
            Builders<NotificationsAggregate>.Filter.Eq(x => x.Type, NotificationType.User),
            Builders<NotificationsAggregate>.Filter.Eq(x => x.WasSuccess, true),
            Builders<NotificationsAggregate>.Filter.Eq(x => x.DeliveredAt, null)
        );

        var sort = Builders<NotificationsAggregate>.Sort.Ascending(x => x.SentAt);

        var collection = base.GetCollection<NotificationsAggregate>();
        var cursor = await collection.FindAsync(filter, new FindOptions<NotificationsAggregate>
        {
            Sort = sort
        }, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task MarkAsDeliveredAsync(Guid notificationId, Instant deliveredAt, Guid updateBy, CancellationToken cancellationToken)
    {
        var filter = Builders<NotificationsAggregate>.Filter.Eq(x => x.Id, notificationId);
        var update = Builders<NotificationsAggregate>.Update
            .Set(x => x.DeliveredAt, deliveredAt)
            .Set(x => x.UpdatedBy, updateBy)
            .Set(x => x.UpdatedAt, SystemClock.Instance.GetCurrentInstant());

        var collection = base.GetCollection<NotificationsAggregate>();
        await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// El filtro de la bandeja: dirigidas a mi, o a un rol que tengo, o a toda mi copropiedad.
    /// </summary>
    /// <remarks>
    /// <b>El tenant va fuera del OR, no dentro.</b> Si estuviera dentro, un aviso de otra copropiedad
    /// dirigido a mi userId me alcanzaria: seria la misma fuga que se cerro en el hub al quitar
    /// <c>Clients.All</c>, reabierta por la puerta de la lectura.
    /// <para>
    /// Es <c>static</c> y publico para que el contador de la campana use exactamente este filtro y no
    /// una copia que envejezca por su cuenta.
    /// </para>
    /// </remarks>
    public static FilterDefinition<NotificationsAggregate> BuildAudienceFilter(Guid tenant, Guid userId, string[] roles)
    {
        var builder = Builders<NotificationsAggregate>.Filter;

        var paraMi = builder.And(
            builder.Eq(x => x.Audience!.Kind, AudienceKind.User),
            builder.AnyEq(x => x.Audience!.Values, userId.ToString()));

        var paraMisRoles = builder.And(
            builder.Eq(x => x.Audience!.Kind, AudienceKind.Role),
            builder.AnyIn(x => x.Audience!.Values, roles ?? []));

        var paraTodos = builder.Eq(x => x.Audience!.Kind, AudienceKind.Tenant);

        return builder.And(
            builder.Eq(x => x.Tenant, tenant),
            builder.Ne(x => x.Audience, null),
            builder.Or(paraMi, paraMisRoles, paraTodos));
    }

    /// <summary>
    /// El filtro completo de la bandeja: la audiencia, mas lo que pida quien consulta.
    /// </summary>
    /// <remarks>
    /// <b>La audiencia va primero y siempre.</b> Lo que llegue en <paramref name="criteria"/> se le suma
    /// con <c>AND</c>, asi que solo puede estrechar la bandeja: ningun <c>filters</c> de la URL puede
    /// sacar un aviso dirigido a otro. Sustituir uno por otro, o combinarlos con <c>OR</c>, seria la
    /// misma fuga que la fase 2 cerro en el hub, reabierta por la puerta de la lectura.
    /// <para>
    /// Es <c>static</c> y publico para poder comprobar el filtro real renderizado a BSON, y no una copia
    /// escrita a mano que envejeceria por su cuenta.
    /// </para>
    /// </remarks>
    /// <param name="tenant">La copropiedad del lector.</param>
    /// <param name="userId">El lector.</param>
    /// <param name="roles">Los roles que el lector trae en su token.</param>
    /// <param name="criteria">Lo que pide quien consulta.</param>
    /// <param name="excluir">Avisos a descartar, para el filtro de "solo sin leer".</param>
    /// <param name="openOnly">Solo los avisos sin cerrar.</param>
    public static FilterDefinition<NotificationsAggregate> BuildInboxFilter(Guid tenant, Guid userId, string[] roles, C.Criteria criteria, IReadOnlyCollection<Guid>? excluir, bool openOnly = false)
    {
        var builder = Builders<NotificationsAggregate>.Filter;

        var filtro = BuildAudienceFilter(tenant, userId, roles);

        if (!string.IsNullOrWhiteSpace(criteria.Filters))
            filtro = builder.And(filtro, builder.Where(criteria.GetFilterExpression<NotificationsAggregate>()));

        // El descarte de lo ya leido se hace en la consulta, no despues de paginar: filtrarlo en memoria
        // dejaria el total contando filas que el usuario no va a ver y la ultima pagina corta.
        if (excluir is { Count: > 0 })
            filtro = builder.And(filtro, builder.Nin(x => x.Id, excluir));

        // Un aviso cerrado ya no espera nada de nadie. Los guardados antes no traen el campo: Eq null los toma como abiertos.
        if (openOnly)
            filtro = builder.And(filtro, builder.Eq(x => x.ResolvedAt, null));

        return filtro;
    }

    /// <inheritdoc/>
    public async Task<Pagination<NotificationsAggregate>> GetInboxAsync(Guid tenant, Guid userId, string[] roles, C.Criteria criteria, IReadOnlyCollection<Guid>? excluir, bool openOnly, CancellationToken cancellationToken)
    {
        var collection = base.GetCollection<NotificationsAggregate>();

        var filtro = BuildInboxFilter(tenant, userId, roles, criteria, excluir, openOnly);

        var totalCount = await collection.CountDocumentsAsync(filtro, cancellationToken: cancellationToken);

        var query = collection.Find(filtro);

        var ordenar = criteria.GetSortByExpression<NotificationsAggregate>();

        if (ordenar is not null)
            query = criteria.OrderType == C.OrderTypes.Ascending
                ? query.SortBy(ordenar)
                : query.SortByDescending(ordenar);
        else
            // Por cuando ocurrio el hecho, no por cuando se guardo: un emisor que reintenta minutos
            // despues no debe colarse por delante en la bandeja.
            query = query.SortByDescending(x => x.OccurredAt);

        if (criteria.Skip.HasValue)
            query = query.Skip(criteria.Skip.Value);

        if (criteria.Limit.HasValue)
            query = query.Limit(criteria.Limit.Value);

        var data = await query.ToListAsync(cancellationToken);

        return Pagination<NotificationsAggregate>.Create(data, totalCount, criteria.Limit, criteria.Skip);
    }

    /// <summary>
    /// The filter of the inbox kinds: the unfiltered inbox, so the list never offers a kind the inbox would not show.
    /// </summary>
    /// <remarks>
    /// It is the inbox filter itself, not a copy: tenant outside the OR, and the user, role and tenant-wide branches. Without
    /// it, the distinct would return the kinds of every user and every tenant.
    /// </remarks>
    /// <param name="tenant">The reader's tenant.</param>
    /// <param name="userId">The reader.</param>
    /// <param name="roles">The reader's roles in that tenant.</param>
    public static FilterDefinition<NotificationsAggregate> BuildKindsFilter(Guid tenant, Guid userId, string[] roles)
        => BuildInboxFilter(tenant, userId, roles, new C.Criteria(), null);

    /// <inheritdoc/>
    public async Task<string[]> GetInboxKindsAsync(Guid tenant, Guid userId, string[] roles, CancellationToken cancellationToken)
    {
        var cursor = await base.GetCollection<NotificationsAggregate>()
            .DistinctAsync(x => x.Kind, BuildKindsFilter(tenant, userId, roles), cancellationToken: cancellationToken);

        var kinds = await cursor.ToListAsync(cancellationToken);

        return [.. kinds.Where(x => !string.IsNullOrWhiteSpace(x)).Order(StringComparer.Ordinal)];
    }

    /// <inheritdoc/>
    public Task<long> CountPendingAsync(Guid tenant, Guid userId, string[] roles, IReadOnlyCollection<Guid> leidos, CancellationToken cancellationToken)
        => base.GetCollection<NotificationsAggregate>()
            .CountDocumentsAsync(BuildPendingFilter(tenant, userId, roles, leidos), cancellationToken: cancellationToken);

    /// <summary>
    /// El filtro del contador de la campana: lo que me alcanza, sin leer y sin cerrar.
    /// </summary>
    /// <remarks>
    /// Antes se contaba lo que alcanza y se restaban los acuses. Con avisos cerrados eso deja de cuadrar: un aviso cerrado
    /// que ademas se leyo se restaria dos veces. Contar en una sola consulta lo que esta abierto y sin acuse da el numero
    /// exacto que pinta la bandeja con "solo sin leer".
    /// </remarks>
    /// <param name="tenant">La copropiedad del lector.</param>
    /// <param name="userId">El lector.</param>
    /// <param name="roles">Sus roles en esa copropiedad.</param>
    /// <param name="leidos">Los avisos que ya leyo.</param>
    public static FilterDefinition<NotificationsAggregate> BuildPendingFilter(Guid tenant, Guid userId, string[] roles, IReadOnlyCollection<Guid> leidos)
        => BuildInboxFilter(tenant, userId, roles, new C.Criteria(), leidos, openOnly: true);

    /// <summary>
    /// El filtro de los avisos que cierra una tarea hecha: de la copropiedad, de esos tipos, que llevan a ese recurso, de
    /// hechos anteriores o iguales al cierre y todavia abiertos.
    /// </summary>
    /// <param name="tenant">La copropiedad.</param>
    /// <param name="kinds">Los tipos de aviso.</param>
    /// <param name="module">El modulo del recurso.</param>
    /// <param name="aggregateId">El recurso.</param>
    /// <param name="resolvedAt">Cuando se hizo la tarea.</param>
    public static FilterDefinition<NotificationsAggregate> BuildResolveFilter(Guid tenant, IReadOnlyCollection<string> kinds, string module, string aggregateId, Instant resolvedAt)
    {
        var builder = Builders<NotificationsAggregate>.Filter;

        return builder.And(
            builder.Eq(x => x.Tenant, tenant),
            builder.In(x => x.Kind, kinds),
            builder.Eq(x => x.Resource!.Module, module),
            builder.Eq(x => x.Resource!.AggregateId, aggregateId),
            builder.Lte(x => x.OccurredAt, resolvedAt),
            builder.Eq(x => x.ResolvedAt, null));
    }

    /// <inheritdoc/>
    public async Task<long> ResolveAsync(Guid tenant, IReadOnlyCollection<string> kinds, string module, string aggregateId, Guid resolvedBy, Instant resolvedAt, CancellationToken cancellationToken)
    {
        var update = Builders<NotificationsAggregate>.Update
            .Set(x => x.ResolvedAt, resolvedAt)
            .Set(x => x.ResolvedBy, resolvedBy)
            .Set(x => x.UpdatedBy, resolvedBy)
            .Set(x => x.UpdatedAt, SystemClock.Instance.GetCurrentInstant());

        var result = await base.GetCollection<NotificationsAggregate>()
            .UpdateManyAsync(BuildResolveFilter(tenant, kinds, module, aggregateId, resolvedAt), update, cancellationToken: cancellationToken);

        return result.ModifiedCount;
    }
}
