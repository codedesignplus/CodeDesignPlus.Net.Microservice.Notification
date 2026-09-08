using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.Notify;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.PushLive;
using CodeDesignPlus.Net.Microservice.Notification.Domain;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Repositories;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;
using CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects;
using Moq;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Test.Notifications;

/// <summary>
/// Cubre la diferencia entre el canal efimero y el aviso durable.
/// </summary>
/// <remarks>
/// Es el reparto que da sentido a toda la fase: lo que pierde valor en segundos no se guarda, y lo que
/// le sirve a alguien que no estaba mirando se guarda <b>pase lo que pase con el push</b>.
/// </remarks>
public class StratumTest
{
    private static readonly Guid Id = Guid.Parse("aa000000-0000-4000-8000-000000000001");
    private static readonly Guid Tenant = Guid.Parse("bb000000-0000-4000-8000-000000000002");
    private static readonly Guid Propietario = Guid.Parse("cc000000-0000-4000-8000-000000000003");
    private static readonly Guid SentBy = Guid.Parse("dd000000-0000-4000-8000-000000000004");

    private static readonly Instant Ocurrio = Instant.FromUtc(2026, 9, 8, 15, 30);

    private readonly Mock<INotifierGateway> notifier = new();
    private readonly Mock<INotificationsRepository> repository = new();

    // ── Canal efimero ────────────────────────────────────────────────────────

    [Fact]
    public async Task APushToAUserPersistsNothing()
    {
        await new PushLiveCommandHandler(notifier.Object)
            .Handle(new PushLiveCommand(Tenant, Propietario, null, "charge.generation.progress", "{}"), default);

        notifier.Verify(x => x.SendToUserAsync(Propietario, "charge.generation.progress", "{}", It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task APushToAGroupQualifiesItWithTheTenant()
    {
        // Sin calificar, el grupo "assembly-42" seria el mismo en todas las copropiedades.
        await new PushLiveCommandHandler(notifier.Object)
            .Handle(new PushLiveCommand(Tenant, null, "assembly-42", "FloorGranted", "{}"), default);

        notifier.Verify(x => x.SendToGroupAsync($"Tenant:{Tenant}:assembly-42", "FloorGranted", "{}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task APushThatFailsDoesNotThrow()
    {
        // Un push perdido es un push perdido: no puede tumbar al emisor, que esta en mitad de su propia
        // operacion de negocio.
        notifier.Setup(x => x.SendToUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("hub caido"));

        var result = await new PushLiveCommandHandler(notifier.Object)
            .Handle(new PushLiveCommand(Tenant, Propietario, null, "charge.generation.progress", "{}"), default);

        Assert.False(result);
    }

    // ── Aviso durable ────────────────────────────────────────────────────────

    [Fact]
    public async Task ANoticeIsPersistedAndPushed()
    {
        await BuildNotifyHandler().Handle(NoticeFor(Audience.ForUsers([Propietario])), default);

        repository.Verify(x => x.CreateAsync(It.IsAny<NotificationsAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(x => x.SendToUserAsync(Propietario, "invoice.issued", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ANoticeIsPersistedEvenWhenNobodyIsListening()
    {
        // La razon de ser del estrato durable. Si guardar dependiera de empujar, el aviso se perderia justo
        // cuando mas falta hace: cuando el destinatario no esta.
        notifier.Setup(x => x.SendToUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("hub caido"));

        await BuildNotifyHandler().Handle(NoticeFor(Audience.ForUsers([Propietario])), default);

        repository.Verify(x => x.CreateAsync(It.IsAny<NotificationsAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ATenantWideNoticeGoesToTheTenantGroup()
    {
        await BuildNotifyHandler().Handle(NoticeFor(Audience.ForTenant()), default);

        notifier.Verify(x => x.BroadcastAsync(Tenant, "invoice.issued", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ARoleNoticeGoesToOneGroupPerRole()
    {
        await BuildNotifyHandler().Handle(NoticeFor(Audience.ForRoles(["Administrador", "Contador"])), default);

        notifier.Verify(x => x.SendToGroupAsync($"Tenant:{Tenant}:Role:Administrador", "invoice.issued", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(x => x.SendToGroupAsync($"Tenant:{Tenant}:Role:Contador", "invoice.issued", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private NotifyCommandHandler BuildNotifyHandler() => new(notifier.Object, repository.Object);

    private static NotifyCommand NoticeFor(Audience audience) => new(
        Id, Tenant, audience, "invoice.issued", "Cuenta de cobro emitida",
        "Su cuota de septiembre ya esta disponible.",
        ResourceRef.Create("invoicing", Id.ToString()), "{}", SentBy, Ocurrio);
}
