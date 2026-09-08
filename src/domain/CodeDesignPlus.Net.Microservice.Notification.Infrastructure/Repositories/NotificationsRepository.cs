namespace CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Repositories;

public class NotificationsRepository(IServiceProvider serviceProvider, IOptions<MongoOptions> mongoOptions, ILogger<NotificationsRepository> logger)
    : RepositoryBase(serviceProvider, mongoOptions, logger), INotificationsRepository
{
    public async Task<List<NotificationsAggregate>> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var filter = Builders<NotificationsAggregate>.Filter.And(
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

    public async Task<List<NotificationsAggregate>> GetInboxAsync(Guid tenant, Guid userId, string[] roles, int page, int size, CancellationToken cancellationToken)
    {
        var collection = base.GetCollection<NotificationsAggregate>();

        var cursor = await collection.FindAsync(
            BuildAudienceFilter(tenant, userId, roles),
            new FindOptions<NotificationsAggregate>
            {
                // Por cuando ocurrio el hecho, no por cuando se guardo: un emisor que reintenta minutos
                // despues no debe colarse por delante en la bandeja.
                Sort = Builders<NotificationsAggregate>.Sort.Descending(x => x.OccurredAt),
                Skip = page * size,
                Limit = size
            },
            cancellationToken);

        return await cursor.ToListAsync(cancellationToken);
    }

    public Task<long> CountInboxAsync(Guid tenant, Guid userId, string[] roles, CancellationToken cancellationToken)
        => base.GetCollection<NotificationsAggregate>()
            .CountDocumentsAsync(BuildAudienceFilter(tenant, userId, roles), cancellationToken: cancellationToken);
}
