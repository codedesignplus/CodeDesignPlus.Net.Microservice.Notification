using CodeDesignPlus.Net.Microservice.Notification.Domain.Enums;
using CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Repositories;
using CodeDesignPlus.Net.Mongo.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Test.Repositories;

/// <summary>
/// Cubre la forma del filtro de la bandeja, que es donde vive la separacion entre copropiedades.
/// </summary>
/// <remarks>
/// La audiencia se guarda como descriptor al escribir y se resuelve al leer. Lo que se comprueba aqui es
/// el <b>filtro real</b>, renderizado a BSON: no una condicion equivalente escrita en LINQ, que seria una
/// copia y envejeceria por su cuenta sin que nadie se enterara.
/// <para>
/// La asercion que de verdad importa es la del tenant. Tiene que quedar <b>fuera</b> del OR: si cayera
/// dentro, un aviso de otra copropiedad dirigido a mi userId me alcanzaria, y seria la misma fuga que la
/// fase 2 cerro en el hub al quitar <c>Clients.All</c>, reabierta por la puerta de la lectura.
/// </para>
/// <para>
/// El driver aplana el <c>And</c> de campos distintos, asi que el documento renderizado no lleva
/// <c>$and</c>: queda <c>{ Tenant, Audience: { $ne: null }, $or: [...] }</c>. Estar al primer nivel
/// <b>es</b> estar fuera del OR.
/// </para>
/// <para>
/// Lo que esto <b>no</b> prueba es que Mongo interprete el filtro como esperamos; eso corresponde a la
/// prueba de integracion contra un Mongo de verdad.
/// </para>
/// </remarks>
public class InboxFilterTest
{
    private static readonly Guid Tenant = Guid.Parse("aa000000-0000-4000-8000-000000000001");
    private static readonly Guid Yo = Guid.Parse("bb000000-0000-4000-8000-000000000002");

    private static readonly string[] MisRoles = ["Propietario", "Consejo"];

    static InboxFilterTest()
    {
        // Los Guid se serializan como binario estandar. Lo registra el SDK al arrancar la aplicacion; en
        // una prueba unitaria no hay arranque, y sin esto renderizar el filtro lanza
        // "GuidSerializer cannot serialize a Guid when GuidRepresentation is Unspecified".
        MongoSerializerRegistration.RegisterSerializers();
    }

    private static BsonDocument Render(Guid tenant, Guid userId, string[] roles)
    {
        var filtro = NotificationsRepository.BuildAudienceFilter(tenant, userId, roles);

        return filtro.Render(new RenderArgs<NotificationsAggregate>(
            BsonSerializer.SerializerRegistry.GetSerializer<NotificationsAggregate>(),
            BsonSerializer.SerializerRegistry));
    }

    /// <summary>Las tres formas de que un aviso me alcance.</summary>
    private static BsonArray RamasDelOr(BsonDocument rendered) => rendered["$or"].AsBsonArray;

    /// <summary>La rama del OR que corresponde a un tipo de audiencia.</summary>
    private static BsonDocument Rama(BsonDocument rendered, AudienceKind kind)
        => RamasDelOr(rendered).Select(x => x.AsBsonDocument).Single(x => x.GetValue("Audience.Kind", null) == (int)kind);

    [Fact]
    public void TheTenantIsFilteredOutsideTheOr()
    {
        // Si el tenant cayera dentro del OR, un aviso de otra copropiedad dirigido a mi userId me
        // alcanzaria. Es el punto entero de esta prueba.
        var rendered = Render(Tenant, Yo, MisRoles);

        Assert.True(rendered.Contains("Tenant"), $"El tenant no esta al primer nivel: {rendered}");
        Assert.Equal(new BsonBinaryData(Tenant, GuidRepresentation.Standard), rendered["Tenant"]);
    }

    [Fact]
    public void TheOrHasExactlyThreeWaysToReachMe()
    {
        // Dirigido a mi, a un rol que tengo, o a toda la copropiedad. Ni una mas: una cuarta rama seria
        // una via de entrega que nadie decidio.
        Assert.Equal(3, RamasDelOr(Render(Tenant, Yo, MisRoles)).Count);
    }

    [Fact]
    public void ANoticeAddressedToMeMatchesMyUserId()
    {
        var rama = Rama(Render(Tenant, Yo, MisRoles), AudienceKind.User);

        Assert.Equal(Yo.ToString(), rama["Audience.Values"].AsString);
    }

    [Fact]
    public void ANoticeForMyRolesMatchesEveryRoleInMyToken()
    {
        // Los roles llegan del JWT del lector. Si el filtro solo mirara el primero, un aviso al Consejo no
        // le llegaria a quien es propietario y consejero a la vez.
        var rama = Rama(Render(Tenant, Yo, MisRoles), AudienceKind.Role);

        Assert.Equal(MisRoles, rama["Audience.Values"]["$in"].AsBsonArray.Select(x => x.AsString));
    }

    [Fact]
    public void ATenantWideNoticeNeedsNoValues()
    {
        // Un aviso a toda la copropiedad no lleva destinatarios: exigirle valores lo dejaria sin entregar.
        var rama = Rama(Render(Tenant, Yo, MisRoles), AudienceKind.Tenant);

        Assert.Single(rama.Elements);
    }

    [Fact]
    public void AReaderWithoutRolesStillGetsTheirOwnNotices()
    {
        // Un residente recien invitado puede no tener ningun rol. El filtro tiene que seguir siendo valido:
        // sin la guarda, AnyIn con null revienta al construirlo.
        var rendered = Render(Tenant, Yo, null!);

        Assert.Equal(3, RamasDelOr(rendered).Count);
        Assert.Empty(Rama(rendered, AudienceKind.Role)["Audience.Values"]["$in"].AsBsonArray);
    }

    [Fact]
    public void ANoticeWithoutAnAudienceIsExcluded()
    {
        // Lo que se guardo cuando todo se guardaba no tiene audiencia. Sin esta condicion, cada progreso de
        // cuota extraordinaria de los que llenaron Mongo apareceria en la bandeja de todo el mundo.
        var rendered = Render(Tenant, Yo, MisRoles);

        Assert.True(rendered.Contains("Audience"), $"No se excluyen los avisos sin audiencia: {rendered}");
        Assert.Equal(BsonNull.Value, rendered["Audience"]["$ne"]);
    }

    /// <summary>El filtro completo, con lo que pida quien consulta encima de la audiencia.</summary>
    private static BsonDocument RenderConCriteria(C.Criteria criteria, IReadOnlyCollection<Guid>? excluir = null)
    {
        var filtro = NotificationsRepository.BuildInboxFilter(Tenant, Yo, MisRoles, criteria, excluir);

        return filtro.Render(new RenderArgs<NotificationsAggregate>(
            BsonSerializer.SerializerRegistry.GetSerializer<NotificationsAggregate>(),
            BsonSerializer.SerializerRegistry));
    }

    [Fact]
    public void CriteriaCanNarrowTheInbox()
    {
        // El caso legitimo: filtrar por tipo desde la tabla. El driver aplana el And de campos distintos,
        // asi que `Kind` queda al lado de la audiencia, no en lugar de ella.
        var rendered = RenderConCriteria(new C.Criteria { Filters = "kind=invoice.issued" });

        Assert.Equal("invoice.issued", rendered["Kind"].AsString);
        Assert.Equal(new BsonBinaryData(Tenant, GuidRepresentation.Standard), rendered["Tenant"]);
        Assert.Equal(3, RamasDelOr(rendered).Count);
    }

    [Fact]
    public void CriteriaCannotReplaceTheTenant()
    {
        // La violacion deliberada: quien consulta pide la bandeja de otra copropiedad por la URL.
        //
        // No hay forma de que gane. Como el nuestro ya ocupa `Tenant`, el driver no puede aplanar los dos
        // y los deja en un $and: el filtro pide las dos copropiedades a la vez y no devuelve nada. Lo que
        // no ocurre —y es el punto de la prueba— es que el suyo sustituya al nuestro.
        //
        // Si algun dia alguien cambia el AND por un OR, o sustituye el filtro en vez de sumarlo, esta
        // prueba es la que se cae.
        var otra = Guid.Parse("dd000000-0000-4000-8000-000000000004");

        var rendered = RenderConCriteria(new C.Criteria { Filters = $"tenant={otra}" });

        var ramas = rendered["$and"].AsBsonArray.Select(x => x.AsBsonDocument).ToList();

        // El nuestro sigue ahi, y la audiencia entera con el. El suyo se suma como una condicion mas.
        Assert.Contains(ramas, x => x.GetValue("Tenant", null) == new BsonBinaryData(Tenant, GuidRepresentation.Standard));
        Assert.Contains(ramas, x => x.Contains("$or") && x["$or"].AsBsonArray.Count == 3);
    }

    [Fact]
    public void ReadNoticesAreDiscardedInTheQuery()
    {
        // "Solo sin leer" descarta antes de paginar. Si se filtrara en memoria despues de traer la pagina,
        // el total contaria filas que el usuario no va a ver y la ultima pagina saldria corta.
        var leido = Guid.Parse("cc000000-0000-4000-8000-000000000003");

        var rendered = RenderConCriteria(new C.Criteria(), [leido]);

        Assert.Equal(new BsonBinaryData(leido, GuidRepresentation.Standard), rendered["_id"]["$nin"].AsBsonArray[0]);
        Assert.Equal(new BsonBinaryData(Tenant, GuidRepresentation.Standard), rendered["Tenant"]);
    }

    [Fact]
    public void AnEmptyCriteriaLeavesTheAudienceFilterAlone()
    {
        // Sin filtros no se envuelve nada: la bandeja sin filtrar tiene que rendir exactamente el mismo
        // documento que la audiencia sola.
        Assert.Equal(Render(Tenant, Yo, MisRoles), RenderConCriteria(new C.Criteria()));
    }
}
