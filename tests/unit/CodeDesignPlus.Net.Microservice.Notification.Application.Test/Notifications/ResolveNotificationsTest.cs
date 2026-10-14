using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.Notify;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.ResolveNotifications;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetUnreadCount;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;
using CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Test.Notifications;

/// <summary>
/// Un aviso cuya tarea ya se hizo deja de contar como pendiente en la campana de todos (pendings/325).
/// </summary>
/// <remarks>
/// El caso: «espera tu aprobacion» seguia en la campana despues de aprobar o rechazar el egreso. El servicio no sabe que
/// tarea pedia el aviso: lo cierra quien la hizo, por el tipo del aviso y el recurso al que lleva.
/// </remarks>
public class ResolveNotificationsTest
{
    private const string Kind = "expenses.expense_approval_required";

    private static readonly Guid Tenant = Guid.Parse("bb000000-0000-4000-8000-000000000002");
    private static readonly Guid Approver = Guid.Parse("cc000000-0000-4000-8000-000000000003");
    private static readonly string ExpenseId = "b709b9ec-a9c7-4645-aed0-883f8dc1efb7";

    private static readonly Instant Requested = Instant.FromUtc(2026, 10, 13, 15, 0);
    private static readonly Instant Approved = Instant.FromUtc(2026, 10, 13, 16, 0);

    private readonly Mock<INotificationsRepository> repository = new();
    private readonly Mock<INotifierGateway> notifier = new();
    private readonly List<NotificationResolutionAggregate> resolutions = [];
    private NotificationsAggregate? stored;

    public ResolveNotificationsTest()
    {
        repository
            .Setup(x => x.FindAsync<NotificationResolutionAggregate>(It.IsAny<Guid>(), Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, Guid _, CancellationToken _) => resolutions.FirstOrDefault(x => x.Id == id));

        repository
            .Setup(x => x.CreateAsync(It.IsAny<NotificationResolutionAggregate>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationResolutionAggregate, CancellationToken>((x, _) => resolutions.Add(x))
            .Returns(Task.CompletedTask);

        repository
            .Setup(x => x.CreateAsync(It.IsAny<NotificationsAggregate>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationsAggregate, CancellationToken>((x, _) => stored = x)
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Resolve_ApprovedExpense_ClosesTheOpenNoticesOfThatKindAndResource()
    {
        // Arrange
        repository
            .Setup(x => x.ResolveAsync(Tenant, It.Is<IReadOnlyCollection<string>>(k => k.Single() == Kind), "expenses", ExpenseId, Approver, Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var closed = await new ResolveNotificationsCommandHandler(repository.Object)
            .Handle(new ResolveNotificationsCommand(Tenant, [Kind], "expenses", ExpenseId, Approver, Approved), default);

        // Assert
        Assert.Equal((2L, Approved), (closed, Assert.Single(resolutions).ResolvedAt));
    }

    /// <summary>
    /// El cierre va directo y el aviso por una cola: si el aviso llega despues, de un hecho anterior al cierre, nace cerrado y
    /// no se empuja.
    /// </summary>
    [Fact]
    public async Task Notify_NoticeArrivingAfterItsResolution_IsStoredClosedAndNotPushed()
    {
        // Arrange
        resolutions.Add(NotificationResolutionAggregate.Create(Tenant, Kind, "expenses", ExpenseId, Approver, Approved));

        // Act
        await new NotifyCommandHandler(notifier.Object, repository.Object).Handle(Notice(Requested), default);

        // Assert
        Assert.Equal(Approved, stored!.ResolvedAt);
        notifier.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Si el egreso vuelve a necesitar aprobacion despues del cierre, ese aviso es otra tarea y queda abierto.
    /// </summary>
    [Fact]
    public async Task Notify_NoticeOfALaterFact_StaysOpen()
    {
        // Arrange
        resolutions.Add(NotificationResolutionAggregate.Create(Tenant, Kind, "expenses", ExpenseId, Approver, Approved));

        // Act
        await new NotifyCommandHandler(notifier.Object, repository.Object).Handle(Notice(Approved.Plus(Duration.FromHours(1))), default);

        // Assert
        Assert.Null(stored!.ResolvedAt);
    }

    /// <summary>
    /// La campana cuenta en una consulta lo abierto y sin leer: restar los acuses de lo que alcanza restaria dos veces un aviso
    /// cerrado y leido.
    /// </summary>
    [Fact]
    public async Task UnreadCount_CountsOpenAndUnreadInOneQuery()
    {
        // Arrange
        var reads = new Mock<INotificationReadRepository>();
        var read = new List<Guid> { Guid.NewGuid() };
        reads.Setup(x => x.GetAllReadIdsAsync(Tenant, Approver, It.IsAny<CancellationToken>())).ReturnsAsync(read);

        var user = new Mock<IUserContext>();
        user.SetupGet(x => x.Tenant).Returns(Tenant);
        user.SetupGet(x => x.IdUser).Returns(Approver);

        var directory = new Mock<IRoleDirectory>();
        directory.Setup(x => x.GetRolesAsync(Approver, Tenant, It.IsAny<CancellationToken>())).ReturnsAsync(["council"]);

        repository.Setup(x => x.CountPendingAsync(Tenant, Approver, It.IsAny<string[]>(), read, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        // Act
        var count = await new GetUnreadCountQueryHandler(repository.Object, reads.Object, user.Object, directory.Object).Handle(new GetUnreadCountQuery(), default);

        // Assert
        Assert.Equal(3, count);
    }

    private static NotifyCommand Notice(Instant occurredAt) => new(
        Guid.NewGuid(), Tenant, Audience.ForRoles(["council"]), Kind, "Egreso por aprobar", "Espera tu aprobación.",
        ResourceRef.Create("expenses", ExpenseId), "{}", Guid.Empty, occurredAt);
}
