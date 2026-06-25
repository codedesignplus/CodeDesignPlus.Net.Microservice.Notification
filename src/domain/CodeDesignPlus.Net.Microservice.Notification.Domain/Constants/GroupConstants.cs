namespace CodeDesignPlus.Net.Microservice.Notification.Domain.Constants;

public static class GroupConstants
{
    public const string TenantGroupPrefix = "Tenant";

    public static string BuildTenantGroupName(Guid tenant, string groupName)
        => $"{TenantGroupPrefix}:{tenant}:{groupName}";
}
