namespace CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Repositories;

/// <summary>
/// Los acuses de lectura, en su propia coleccion.
/// </summary>
public class NotificationReadRepository(IServiceProvider serviceProvider, IOptions<MongoOptions> mongoOptions, ILogger<NotificationReadRepository> logger)
    : RepositoryBase(serviceProvider, mongoOptions, logger), INotificationReadRepository
{
    /// <inheritdoc/>
    public Task MarkAsReadAsync(NotificationReadAggregate receipt, CancellationToken cancellationToken)
        => base.GetCollection<NotificationReadAggregate>().ReplaceOneAsync(
            Builders<NotificationReadAggregate>.Filter.Eq(x => x.Id, receipt.Id),
            receipt,
            // Upsert sobre el id determinista: marcar leido dos veces actualiza la misma fila en vez de
            // reventar por clave duplicada.
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

    /// <inheritdoc/>
    public async Task<HashSet<Guid>> GetReadIdsAsync(Guid tenant, Guid userId, IEnumerable<Guid> notificationIds, CancellationToken cancellationToken)
    {
        var ids = notificationIds as IReadOnlyCollection<Guid> ?? [.. notificationIds];

        if (ids.Count == 0)
            return [];

        var filter = Builders<NotificationReadAggregate>.Filter.And(
            Builders<NotificationReadAggregate>.Filter.Eq(x => x.Tenant, tenant),
            Builders<NotificationReadAggregate>.Filter.Eq(x => x.UserId, userId),
            Builders<NotificationReadAggregate>.Filter.In(x => x.NotificationId, ids));

        var cursor = await base.GetCollection<NotificationReadAggregate>()
            .FindAsync(filter, cancellationToken: cancellationToken);

        var acuses = await cursor.ToListAsync(cancellationToken);

        return [.. acuses.Select(x => x.NotificationId)];
    }

    /// <inheritdoc/>
    public Task<long> CountReadAsync(Guid tenant, Guid userId, CancellationToken cancellationToken)
    {
        var filter = Builders<NotificationReadAggregate>.Filter.And(
            Builders<NotificationReadAggregate>.Filter.Eq(x => x.Tenant, tenant),
            Builders<NotificationReadAggregate>.Filter.Eq(x => x.UserId, userId));

        return base.GetCollection<NotificationReadAggregate>()
            .CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }
}
