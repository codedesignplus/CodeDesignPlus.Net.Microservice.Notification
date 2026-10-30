namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetInboxKinds;

/// <summary>
/// Handler for <see cref="GetInboxKindsQuery"/>.
/// </summary>
/// <remarks>
/// Roles come from the <c>IRoleDirectory</c> for the current tenant, exactly as in the inbox: the filter must offer the same
/// kinds the inbox can show, or choosing one would return an empty table.
/// </remarks>
public class GetInboxKindsQueryHandler(
    INotificationsRepository repository,
    IUserContext user,
    IRoleDirectory roleDirectory) : IRequestHandler<GetInboxKindsQuery, string[]>
{
    /// <summary>Returns the distinct kinds that reach the reader in the current tenant.</summary>
    public async Task<string[]> Handle(GetInboxKindsQuery request, CancellationToken cancellationToken)
    {
        var roles = await roleDirectory.GetRolesAsync(user.IdUser, user.Tenant, cancellationToken);

        return await repository.GetInboxKindsAsync(user.Tenant, user.IdUser, roles, cancellationToken);
    }
}
