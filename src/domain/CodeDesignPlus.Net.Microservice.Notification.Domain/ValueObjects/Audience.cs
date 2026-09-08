using Newtonsoft.Json;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Enums;

namespace CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects;

/// <summary>
/// El descriptor de a quien va un aviso, sin resolverlo.
/// </summary>
/// <remarks>
/// La audiencia se guarda como descriptor y se resuelve al **leer**, no al escribir. Resolverla al
/// escribir obligaria a que cada micro supiera los identificadores de usuario detras de un rol
/// —ms-frontdesk conoce la unidad, no al residente— y eso es una llamada cruzada en el path de
/// escritura, que es justo lo que prohibe la regla de fuente de verdad por servicio.
/// <para>
/// Al leer sale gratis: los roles el lector los trae en su propio JWT.
/// </para>
/// </remarks>
public class Audience
{
    /// <summary>Como interpretar <see cref="Values"/>.</summary>
    public AudienceKind Kind { get; private set; }

    /// <summary>Identificadores de usuario si <see cref="Kind"/> es User; nombres de rol si es Role; vacio si es Tenant.</summary>
    public List<string> Values { get; private set; }

    [JsonConstructor]
    private Audience(AudienceKind kind, List<string> values)
    {
        Kind = kind;
        Values = values;
    }

    /// <summary>Dirige el aviso a usuarios concretos.</summary>
    public static Audience ForUsers(IEnumerable<Guid> userIds) => new(AudienceKind.User, [.. userIds.Select(x => x.ToString())]);

    /// <summary>Dirige el aviso a quien tenga alguno de esos roles.</summary>
    public static Audience ForRoles(IEnumerable<string> roles) => new(AudienceKind.Role, [.. roles]);

    /// <summary>Dirige el aviso a toda la copropiedad.</summary>
    public static Audience ForTenant() => new(AudienceKind.Tenant, []);
}
