using CodeDesignPlus.Net.Microservice.Notification.Domain.Enums;

namespace CodeDesignPlus.Net.Microservice.Notification.Domain;

public class NotificationsAggregate(Guid id) : AggregateRootBase(id)
{
    /// <summary>
    /// User identifier to whom the notification is targeted
    /// </summary>
    public Guid? UserId { get; private set; }
    /// <summary>
    /// Type of notification: User, Group, or Broadcast
    /// </summary>
    public NotificationType Type { get; private set; }
    /// <summary>
    /// Name of the group to which the notification is targeted
    /// </summary>
    public string? GroupName { get; private set; }
    /// <summary>
    /// Name of the event that triggered this notification (e.g., "OrderPaymentCompleted", "UserConfigured")
    /// </summary>
    public string? EventName { get; private set; }
    /// <summary>
    /// Preview or snippet of the notification payload, useful if the full payload is large.
    /// </summary>
    public string? PayloadPreview { get; private set; }
    /// <summary>
    /// Timestamp when the notification was sent
    /// </summary>
    public Instant SentAt { get; private set; }
    /// <summary>
    /// Indicates whether the notification was successfully sent
    /// </summary>
    public bool WasSuccess { get; private set; }
    /// <summary>
    /// Reason for failure if the notification was not sent successfully
    /// </summary>
    public string? FailureReason { get; private set; }
    /// <summary>
    /// Timestamp when the notification was delivered to the client
    /// </summary>
    public Instant? DeliveredAt { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    public Guid Tenant { get; private set; }

    public static NotificationsAggregate Create(Guid id, string eventName, NotificationType type, string? payloadPreview, Guid tenant, Guid createdBy)
    {
        var aggregate = new NotificationsAggregate(id)
        {
            EventName = eventName,
            Type = type,
            PayloadPreview = payloadPreview,
            SentAt = SystemClock.Instance.GetCurrentInstant(),
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
            CreatedBy = createdBy,
            Tenant = tenant
        };

        return aggregate;
    }

    public static NotificationsAggregate Create(Guid id, Guid? userId, string eventName, NotificationType type, string? payloadPreview, Guid tenant, Guid createdBy)
    {
        var aggregate = Create(id, eventName, type, payloadPreview, tenant, createdBy);

        aggregate.UserId = userId;

        return aggregate;
    }

    public static NotificationsAggregate Create(Guid id, string groupName, string eventName, NotificationType type, string? payloadPreview, Guid tenant, Guid createdBy)
    {
        var aggregate = Create(id, eventName, type, payloadPreview, tenant, createdBy);

        aggregate.GroupName = groupName;

        return aggregate;
    }

    public void MarkAsSent(Guid? updateBy)
    {
        WasSuccess = true;

        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedBy = updateBy;
    }

    public void MarkAsFailed(string reason, Guid? updateBy)
    {
        WasSuccess = false;
        FailureReason = reason;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedBy = updateBy;
    }

    public void MarkAsDelivered(Instant deliveredAt, Guid updateBy)
    {
        DomainGuard.IsNotNull(DeliveredAt, Errors.NotificationAlreadyDelivered);

        DeliveredAt = deliveredAt;
        UpdatedBy = updateBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }
}
