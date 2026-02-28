namespace CodeDesignPlus.Net.Microservice.Notification.Domain.DomainEvents;

[EventKey<NotificationsAggregate>(1, "NotificationsCreatedDomainEvent")]
public class NotificationsCreatedDomainEvent(
     Guid aggregateId,
     Guid? eventId = null,
     Instant? occurredAt = null,
     Dictionary<string, object>? metadata = null
) : DomainEvent(aggregateId, eventId, occurredAt, metadata)
{
    public static NotificationsCreatedDomainEvent Create(Guid aggregateId)
    {
        return new NotificationsCreatedDomainEvent(aggregateId);
    }
}
