using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.DeliverPendingNotifications;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Repositories;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;
using CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Test.Notifications;

/// <summary>
/// Cubre la reentrega de lo que le llego a alguien mientras no estaba conectado.
/// </summary>
/// <remarks>
/// Tres defectos vivian en este mismo camino: el sobre no coincidia con el de la entrega en vivo, no se
/// filtraba por copropiedad, y el job reportaba exito con fallos.
/// </remarks>
public class DeliverPendingNotificationsHandlerTest
{
    private static readonly Guid Tenant = Guid.Parse("aa000000-0000-4000-8000-000000000001");
    private static readonly Guid Usuario = Guid.Parse("bb000000-0000-4000-8000-000000000002");

    private readonly Mock<INotificationsRepository> repository = new();
    private readonly Mock<INotificationDeliveryService> delivery = new();
    private readonly Mock<IMediator> mediator = new();
    private readonly Mock<ILogger<DeliverPendingNotificationsCommandHandler>> logger = new();

    [Fact]
    public async Task TheHandlerReportsWhatWasActuallyDeliveredAndNotTheTotal()
    {
        // Es el defecto de la regla 23: el log decia "Successfully delivered 12" aunque fallaran las doce,
        // porque contaba pending.Count en vez de los aciertos.
        DadosPendientes(Aviso(), Aviso());

        delivery
            .Setup(x => x.DeliverToConnectionAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("conexion caida"));

        // Y si no llego ninguno, el job no puede terminar en verde.
        await Assert.ThrowsAsync<InvalidOperationException>(() => Handler().Handle(Comando(), CancellationToken.None));
    }

    [Fact]
    public async Task APartialFailureIsReportedAndDoesNotStopTheRest()
    {
        var falla = Aviso();
        var pasa = Aviso();

        DadosPendientes(falla, pasa);

        delivery
            .Setup(x => x.DeliverToConnectionAsync(It.IsAny<string>(), falla.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("conexion caida"));

        var resultado = await Handler().Handle(Comando(), CancellationToken.None);

        Assert.Equal(1, resultado.Delivered);
        Assert.Equal(1, resultado.Failed);
    }

    [Fact]
    public async Task PendingNoticesOfAnotherCopropertyAreNotReplayed()
    {
        // Sin el tenant en la consulta, quien pertenece a dos copropiedades recibia al conectarse los
        // pendientes de ambas.
        DadosPendientes();

        await Handler().Handle(Comando(), CancellationToken.None);

        repository.Verify(x => x.GetPendingByUserIdAsync(Tenant, Usuario, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NothingPendingIsNotAFailure()
    {
        DadosPendientes();

        var resultado = await Handler().Handle(Comando(), CancellationToken.None);

        Assert.Equal(0, resultado.Delivered);
        Assert.Equal(0, resultado.Failed);
    }

    [Fact]
    public async Task EachDeliveredNoticeIsMarkedAsDelivered()
    {
        // Si no se marcara, la siguiente reconexion volveria a reentregar lo mismo, y la campana
        // mostraria repetido lo que ya se vio.
        DadosPendientes(Aviso(), Aviso());

        await Handler().Handle(Comando(), CancellationToken.None);

        mediator.Verify(x => x.Send(It.IsAny<IRequest<Unit>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private void DadosPendientes(params NotificationsAggregate[] avisos)
        => repository
            .Setup(x => x.GetPendingByUserIdAsync(Tenant, Usuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. avisos]);

    private DeliverPendingNotificationsCommandHandler Handler()
        => new(mediator.Object, delivery.Object, logger.Object, repository.Object);

    private static DeliverPendingNotificationsCommand Comando() => new(Tenant, Usuario, "conn-1");

    private static NotificationsAggregate Aviso() => NotificationsAggregate.CreateNotice(
        Guid.NewGuid(), Audience.ForUsers([Usuario]), "invoice.issued", "Cuenta de cobro emitida",
        "Su cuota ya esta disponible.", ResourceRef.Create("invoicing", Guid.NewGuid().ToString()),
        "{}", Tenant, Guid.Empty, Instant.FromUtc(2026, 9, 8, 12, 0));
}
