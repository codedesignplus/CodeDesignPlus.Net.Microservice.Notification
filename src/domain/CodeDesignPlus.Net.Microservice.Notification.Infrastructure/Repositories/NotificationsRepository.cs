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
}