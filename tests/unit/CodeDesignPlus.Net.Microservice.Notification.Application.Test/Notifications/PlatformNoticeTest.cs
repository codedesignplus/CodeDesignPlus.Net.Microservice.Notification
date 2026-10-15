using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.Notify;
using CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Test.Notifications;

/// <summary>
/// Un aviso a personas o a roles puede ir sin copropiedad: es un aviso de plataforma, el que leen los roles de plataforma
/// fuera de toda copropiedad. Uno a toda la copropiedad, no.
/// </summary>
public class PlatformNoticeTest
{
    private static readonly Guid Id = Guid.Parse("aa000000-0000-4000-8000-000000000011");
    private static readonly Instant OccurredAt = Instant.FromUtc(2026, 11, 2, 6, 30);

    [Fact]
    public void Validate_RoleNoticeWithoutTenant_IsValid()
    {
        // Arrange
        var command = new NotifyCommand(Id, Guid.Empty, Audience.ForRoles(["20000000-0000-0000-0000-000000000010"]), "platform.notice", "Aviso", "Texto", null, null, Guid.Empty, OccurredAt);

        // Act
        var result = new Validator().Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_TenantNoticeWithoutTenant_IsInvalid()
    {
        // Arrange
        var command = new NotifyCommand(Id, Guid.Empty, Audience.ForTenant(), "platform.notice", "Aviso", "Texto", null, null, Guid.Empty, OccurredAt);

        // Act
        var result = new Validator().Validate(command);

        // Assert
        Assert.False(result.IsValid);
    }
}
