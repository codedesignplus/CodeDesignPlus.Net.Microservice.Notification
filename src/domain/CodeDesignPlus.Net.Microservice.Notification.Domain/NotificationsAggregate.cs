using CodeDesignPlus.Net.Microservice.Notification.Domain.Enums;

namespace CodeDesignPlus.Net.Microservice.Notification.Domain;

public class NotificationsAggregate(Guid id) : AggregateRoot(id)
{
    /// <summary>
    /// Aggregate root for notifications, representing a notification event sent to a user, group, or broadcasted to all.
    /// </summary>
    public string Target { get; private set; } = string.Empty;
    /// <summary>
    /// Type of notification: User, Group, or Broadcast
    /// </summary>
    public NotificationType Type { get; private set; }
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

    public static NotificationsAggregate Create(Guid id, string target, NotificationType type, string? payloadPreview, Instant SentAt, Guid tenant, Guid createdBy)
    {
        var aggregate = new NotificationsAggregate(id)
        {
            Target = target,
            Type = type,
            PayloadPreview = payloadPreview,
            SentAt = SentAt,
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
            CreatedBy = createdBy
        };

        return aggregate;
    }


    public void MarkAsSent(Guid? updateBy)
    {
        WasSuccess = true;

        UpdatedAt =  SystemClock.Instance.GetCurrentInstant();
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
