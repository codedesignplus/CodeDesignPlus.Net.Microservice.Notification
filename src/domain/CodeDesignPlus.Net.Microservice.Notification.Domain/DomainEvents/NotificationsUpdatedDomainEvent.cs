namespace CodeDesignPlus.Net.Microservice.Notification.Domain.DomainEvents;

[EventKey<NotificationsAggregate>(1, "NotificationsUpdatedDomainEvent")]
public class NotificationsUpdatedDomainEvent(
     Guid aggregateId,
     Guid? eventId = null,
     Instant? occurredAt = null,
     Dictionary<string, object>? metadata = null
) : DomainEvent(aggregateId, eventId, occurredAt, metadata)
{
    public static NotificationsUpdatedDomainEvent Create(Guid aggregateId)
    {
        return new NotificationsUpdatedDomainEvent(aggregateId);
    }
}
