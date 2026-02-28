namespace CodeDesignPlus.Net.Microservice.Notification.Domain.DomainEvents;

[EventKey<NotificationsAggregate>(1, "NotificationsDeletedDomainEvent")]
public class NotificationsDeletedDomainEvent(
     Guid aggregateId,
     Guid? eventId = null,
     Instant? occurredAt = null,
     Dictionary<string, object>? metadata = null
) : DomainEvent(aggregateId, eventId, occurredAt, metadata)
{
    public static NotificationsDeletedDomainEvent Create(Guid aggregateId)
    {
        return new NotificationsDeletedDomainEvent(aggregateId);
    }
}
