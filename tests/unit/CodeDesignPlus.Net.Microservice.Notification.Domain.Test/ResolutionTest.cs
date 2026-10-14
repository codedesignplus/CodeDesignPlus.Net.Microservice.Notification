using CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects;

namespace CodeDesignPlus.Net.Microservice.Notification.Domain.Test;

/// <summary>
/// Cerrar un aviso es idempotente y el cierre solo cubre los hechos anteriores (pendings/325).
/// </summary>
public class ResolutionTest
{
    private static readonly Guid Tenant = Guid.Parse("bb000000-0000-4000-8000-000000000002");
    private static readonly Guid By = Guid.Parse("cc000000-0000-4000-8000-000000000003");
    private static readonly Instant First = Instant.FromUtc(2026, 10, 13, 16, 0);

    [Fact]
    public void Resolve_AlreadyResolved_KeepsTheFirstResolution()
    {
        // Arrange
        var notice = NotificationsAggregate.CreateNotice(
            Guid.NewGuid(), Audience.ForTenant(), "expenses.expense_approval_required", "Egreso por aprobar", "b",
            ResourceRef.Create("expenses", "x"), "{}", Tenant, By, First.Minus(Duration.FromHours(1)));
        notice.Resolve(By, First);

        // Act
        notice.Resolve(Guid.NewGuid(), First.Plus(Duration.FromHours(1)));

        // Assert
        Assert.Equal((First, (Guid?)By), (notice.ResolvedAt, notice.ResolvedBy));
    }

    [Fact]
    public void Resolution_SameKey_HasTheSameId()
    {
        // Act
        var a = NotificationResolutionAggregate.Create(Tenant, "k", "expenses", "x", By, First);
        var b = NotificationResolutionAggregate.Create(Tenant, "k", "expenses", "x", By, First.Plus(Duration.FromHours(2)));

        // Assert
        Assert.Equal(a.Id, b.Id);
    }

    [Fact]
    public void Advance_OlderResolution_ChangesNothing()
    {
        // Arrange
        var resolution = NotificationResolutionAggregate.Create(Tenant, "k", "expenses", "x", By, First);

        // Act
        var moved = resolution.Advance(By, First.Minus(Duration.FromMinutes(5)));

        // Assert
        Assert.Equal((false, First), (moved, resolution.ResolvedAt));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void Covers_OnlyFactsUpToTheResolution(int minutes, bool expected)
    {
        // Arrange
        var resolution = NotificationResolutionAggregate.Create(Tenant, "k", "expenses", "x", By, First);

        // Act
        var covers = resolution.Covers(First.Plus(Duration.FromMinutes(minutes)));

        // Assert
        Assert.Equal(expected, covers);
    }
}
