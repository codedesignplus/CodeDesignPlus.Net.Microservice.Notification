using CodeDesignPlus.Net.Microservice.Notification.Domain.Enums;
using CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects;

namespace CodeDesignPlus.Net.Microservice.Notification.Domain.Test;

/// <summary>
/// Cubre la forma de un aviso durable: a quien va, de que tipo es y a donde lleva.
/// </summary>
/// <remarks>
/// Es lo que separa el estrato durable del efimero. Hasta ahora todo se guardaba igual —un reparto de
/// cuota extraordinaria de 200 unidades dejaba 200 documentos de progreso que nadie leia— y nada de lo
/// guardado servia para una bandeja: no habia ni titulo, ni destinatario, ni destino del clic.
/// </remarks>
public class NoticeTest
{
    private static readonly Guid Id = Guid.Parse("aa000000-0000-4000-8000-000000000001");
    private static readonly Guid Tenant = Guid.Parse("bb000000-0000-4000-8000-000000000002");
    private static readonly Guid Propietario = Guid.Parse("cc000000-0000-4000-8000-000000000003");
    private static readonly Guid SentBy = Guid.Parse("dd000000-0000-4000-8000-000000000004");

    private static readonly Instant Ocurrio = Instant.FromUtc(2026, 9, 8, 15, 30);

    [Fact]
    public void ANoticeForOneUserKeepsThatUserInItsAudience()
    {
        var aviso = NotificationsAggregate.CreateNotice(
            Id, Audience.ForUsers([Propietario]), "invoice.issued",
            "Cuenta de cobro emitida", "Su cuota de septiembre ya esta disponible.",
            ResourceRef.Create("invoicing", Id.ToString()), "{}", Tenant, SentBy, Ocurrio);

        Assert.Equal(AudienceKind.User, aviso.Audience!.Kind);
        Assert.Equal([Propietario.ToString()], aviso.Audience.Values);
    }

    [Fact]
    public void ANoticeForRolesKeepsTheRoleNames()
    {
        var aviso = NotificationsAggregate.CreateNotice(
            Id, Audience.ForRoles(["Administrador", "Contador"]), "pqrs.created",
            "Nueva PQRS", "PQRS-142 entro al sistema.",
            ResourceRef.Create("communitylife", Id.ToString()), "{}", Tenant, SentBy, Ocurrio);

        Assert.Equal(AudienceKind.Role, aviso.Audience!.Kind);
        Assert.Equal(["Administrador", "Contador"], aviso.Audience.Values);
    }

    [Fact]
    public void ANoticeForTheWholeTenantCarriesNoValues()
    {
        var aviso = NoticeForTenant();

        Assert.Equal(AudienceKind.Tenant, aviso.Audience!.Kind);
        Assert.Empty(aviso.Audience.Values);
    }

    [Fact]
    public void ANoticeKeepsWhenItHappenedAndNotWhenItWasStored()
    {
        // El emisor puede reintentar minutos despues; el aviso sigue siendo del momento del hecho, y es lo
        // que la bandeja ordena.
        var aviso = NoticeForTenant();

        Assert.Equal(Ocurrio, aviso.OccurredAt);
        Assert.NotEqual(Ocurrio, aviso.CreatedAt);
    }

    [Fact]
    public void ANoticeKeepsWhereTheClickLeads()
    {
        var aviso = NotificationsAggregate.CreateNotice(
            Id, Audience.ForUsers([Propietario]), "invoice.issued", "t", "b",
            ResourceRef.Create("invoicing", "ff0d5d38-0db6-5edf-8145-e8d7565cd3b5"), "{}", Tenant, SentBy, Ocurrio);

        Assert.Equal("invoicing", aviso.Resource!.Module);
        Assert.Equal("ff0d5d38-0db6-5edf-8145-e8d7565cd3b5", aviso.Resource.AggregateId);
    }

    [Fact]
    public void ANoticeWithoutAKindIsRejected()
    {
        // Sin kind la bandeja no sabe que texto ni que icono pintar, y el guardrail del catalogo no puede
        // cruzarlo contra las constantes del SDK.
        Assert.Throws<CodeDesignPlusException>(() => NotificationsAggregate.CreateNotice(
            Id, Audience.ForTenant(), "", "t", "b", null, "{}", Tenant, SentBy, Ocurrio));
    }

    [Fact]
    public void ANoticeWithoutATitleIsRejected()
    {
        // El titulo es el respaldo cuando el frontend no conoce el kind. Sin el, un kind desconocido no
        // tendria nada que pintar y el aviso desapareceria en silencio.
        Assert.Throws<CodeDesignPlusException>(() => NotificationsAggregate.CreateNotice(
            Id, Audience.ForTenant(), "admin.broadcast", "", "b", null, "{}", Tenant, SentBy, Ocurrio));
    }

    [Fact]
    public void ANoticeWithoutAResourceIsValid()
    {
        // Un aviso general no lleva a ninguna parte, y eso es legitimo: la bandeja lo muestra sin enlace.
        var aviso = NoticeForTenant();

        Assert.Null(aviso.Resource);
    }

    private static NotificationsAggregate NoticeForTenant() => NotificationsAggregate.CreateNotice(
        Id, Audience.ForTenant(), "admin.broadcast", "Corte de agua", "Manana de 8 a 12.",
        null, "{}", Tenant, SentBy, Ocurrio);
}
