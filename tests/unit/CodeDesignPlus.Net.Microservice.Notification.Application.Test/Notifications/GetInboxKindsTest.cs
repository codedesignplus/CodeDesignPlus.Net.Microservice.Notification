using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetInboxKinds;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Test.Notifications;

/// <summary>
/// The inbox "kind" filter offers only the kinds the reader has (pendings/718).
/// </summary>
public class GetInboxKindsTest
{
    private static readonly Guid Tenant = Guid.Parse("bb000000-0000-4000-8000-000000000002");
    private static readonly Guid Reader = Guid.Parse("cc000000-0000-4000-8000-000000000003");

    /// <summary>
    /// The kinds are read for the user and tenant of the token, with the reader's roles in that tenant, as in the inbox.
    /// </summary>
    [Fact]
    public async Task Handle_ReaderInTenant_ReturnsKindsForTheirUserTenantAndRoles()
    {
        // Arrange
        var user = new Mock<IUserContext>();
        user.SetupGet(x => x.Tenant).Returns(Tenant);
        user.SetupGet(x => x.IdUser).Returns(Reader);

        string[] roles = ["owner"];
        var directory = new Mock<IRoleDirectory>();
        directory.Setup(x => x.GetRolesAsync(Reader, Tenant, It.IsAny<CancellationToken>())).ReturnsAsync(roles);

        var repository = new Mock<INotificationsRepository>();
        repository
            .Setup(x => x.GetInboxKindsAsync(Tenant, Reader, roles, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["moving.approved", "moving.scheduled"]);

        // Act
        var kinds = await new GetInboxKindsQueryHandler(repository.Object, user.Object, directory.Object).Handle(new GetInboxKindsQuery(), default);

        // Assert
        Assert.Equal(["moving.approved", "moving.scheduled"], kinds);
    }
}
