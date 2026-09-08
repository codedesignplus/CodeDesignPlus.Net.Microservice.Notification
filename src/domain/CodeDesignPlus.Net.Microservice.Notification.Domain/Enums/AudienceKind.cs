namespace CodeDesignPlus.Net.Microservice.Notification.Domain.Enums;

/// <summary>
/// A quien va dirigido un aviso.
/// </summary>
/// <remarks>
/// Es un enum y no un catalogo sembrado, y pasa las tres pruebas de la regla 27:
/// <list type="number">
/// <item>La consulta de la bandeja resuelve una rama de filtro distinta por cada valor —USER filtra por
/// id, ROLE intersecta contra los roles del JWT, TENANT no filtra—, asi que anadir un valor exige
/// escribir el filtro.</item>
/// <item>La lista es nuestra: no la decide la ley, ni la DIAN, ni ningun tercero.</item>
/// <item>Nadie de fuera del equipo puede necesitar anadir una forma de dirigir un mensaje en nuestro
/// propio hub.</item>
/// </list>
/// Su fila del inventario: <c>AudienceKind | ms-notification | enum | NotificationQuery resuelve una rama
/// de filtro por valor</c>.
/// </remarks>
public enum AudienceKind
{
    /// <summary>Dirigido a usuarios concretos. Los valores son sus identificadores.</summary>
    User = 1,

    /// <summary>Dirigido a quien tenga alguno de esos roles en la copropiedad.</summary>
    Role = 2,

    /// <summary>Dirigido a toda la copropiedad. No lleva valores.</summary>
    Tenant = 3
}
