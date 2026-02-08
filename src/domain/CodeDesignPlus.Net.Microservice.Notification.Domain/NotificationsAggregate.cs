using CodeDesignPlus.Net.Microservice.Notification.Domain.Enums;

namespace CodeDesignPlus.Net.Microservice.Notification.Domain;

public class NotificationsAggregate(Guid id) : AggregateRoot(id)
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

    public static NotificationsAggregate Create(Guid id, NotificationType type, string? payloadPreview, Guid tenant, Guid createdBy)
    {
        var aggregate = new NotificationsAggregate(id)
        {
            Type = type,
            PayloadPreview = payloadPreview,
            SentAt = SystemClock.Instance.GetCurrentInstant(),
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
            CreatedBy = createdBy
        };

        return aggregate;
    }

    public static NotificationsAggregate Create(Guid id, Guid? userId, NotificationType type, string? payloadPreview, Guid tenant, Guid createdBy)
    {
        var aggregate = Create(id, type, payloadPreview, tenant, createdBy);

        aggregate.UserId = userId;

        return aggregate;
    }

    public static NotificationsAggregate Create(Guid id, string groupName, NotificationType type, string? payloadPreview, Guid tenant, Guid createdBy)
    {
        var aggregate = Create(id, type, payloadPreview, tenant, createdBy);

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
}
