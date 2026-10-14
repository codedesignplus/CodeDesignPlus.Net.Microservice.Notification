using CodeDesignPlus.Net.Microservice.Notification.Domain.Enums;
using CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects;

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

    /// <summary>A quien va dirigido. Nulo en las notificaciones del canal efimero.</summary>
    public Audience? Audience { get; private set; }

    /// <summary>Clave estable del tipo de aviso, ej. <c>invoice.issued</c>.</summary>
    public string? Kind { get; private set; }

    /// <summary>Titulo de respaldo, para cuando el frontend no conoce el <see cref="Kind"/>.</summary>
    public string? Title { get; private set; }

    /// <summary>Cuerpo de respaldo.</summary>
    public string? Body { get; private set; }

    /// <summary>A donde lleva el clic en la bandeja. Nulo si el aviso no lleva a ninguna parte.</summary>
    public ResourceRef? Resource { get; private set; }

    /// <summary>Cuando ocurrio el hecho, que no es cuando se guardo el aviso.</summary>
    public Instant OccurredAt { get; private set; }

    /// <summary>
    /// Cuando se hizo la tarea que el aviso pedia: desde entonces no cuenta como pendiente en la campana de nadie.
    /// </summary>
    /// <remarks>
    /// Miembro suelto (regla 31): los avisos guardados antes no lo traen y se leen como nulo, que es «abierto». El servicio
    /// no sabe que tarea era: lo cierra quien la hizo, por el tipo del aviso y el recurso al que lleva.
    /// </remarks>
    public Instant? ResolvedAt { get; private set; }

    /// <summary>Quien hizo la tarea, o el usuario de sistema.</summary>
    public Guid? ResolvedBy { get; private set; }

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

    /// <summary>
    /// Crea un aviso durable: lo que entra a la bandeja y sobrevive al reinicio del navegador.
    /// </summary>
    /// <remarks>
    /// Es lo contrario del canal efimero, que no persiste nada. La pregunta que decide cual usar, por cada
    /// punto de llamada: <b>le sirve esto a alguien que no estaba mirando la pantalla?</b>
    /// </remarks>
    public static NotificationsAggregate CreateNotice(
        Guid id, Audience audience, string kind, string title, string body,
        ResourceRef? resource, string? payload, Guid tenant, Guid createdBy, Instant occurredAt)
    {
        DomainGuard.IsNullOrEmpty(kind, Errors.NotificationKindIsRequired);
        DomainGuard.IsNullOrEmpty(title, Errors.NotificationTitleIsRequired);

        return new NotificationsAggregate(id)
        {
            Audience = audience,
            Kind = kind,
            Title = title,
            Body = body,
            Resource = resource,
            PayloadPreview = payload,
            OccurredAt = occurredAt,
            SentAt = SystemClock.Instance.GetCurrentInstant(),
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
            CreatedBy = createdBy,
            Tenant = tenant
        };
    }

    /// <summary>
    /// Cierra el aviso porque la tarea que pedia ya se hizo.
    /// </summary>
    /// <param name="resolvedBy">Quien la hizo.</param>
    /// <param name="resolvedAt">Cuando.</param>
    /// <remarks>Idempotente: un aviso ya cerrado conserva su primer cierre.</remarks>
    public void Resolve(Guid resolvedBy, Instant resolvedAt)
    {
        if (ResolvedAt.HasValue)
        {
            return;
        }

        ResolvedAt = resolvedAt;
        ResolvedBy = resolvedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedBy = resolvedBy;
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
