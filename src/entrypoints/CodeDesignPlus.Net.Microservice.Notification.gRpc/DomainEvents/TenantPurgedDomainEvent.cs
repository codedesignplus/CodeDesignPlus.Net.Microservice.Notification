using CodeDesignPlus.Net.Core.Abstractions;
using CodeDesignPlus.Net.Core.Abstractions.Attributes;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.DomainEvents;

/// <summary>
/// ms-tenants purgó una copropiedad eliminada cuyo plazo para restaurarla venció.
/// </summary>
/// <remarks>
/// Solo se declara lo que se usa: el id de la copropiedad. El resto del evento se ignora al deserializar.
/// </remarks>
[EventKey("TenantAggregate", 1, "TenantPurgedDomainEvent", "ms-tenants", "codedesignplus")]
public class TenantPurgedDomainEvent(
    Guid aggregateId,
    string name,
    Guid? eventId = null,
    Instant? occurredAt = null,
    Dictionary<string, object>? metadata = null
) : DomainEvent(aggregateId, eventId, occurredAt, metadata)
{
    public string Name { get; } = name;

    public static TenantPurgedDomainEvent Create(Guid aggregateId, string name)
    {
        return new TenantPurgedDomainEvent(aggregateId, name);
    }
}
