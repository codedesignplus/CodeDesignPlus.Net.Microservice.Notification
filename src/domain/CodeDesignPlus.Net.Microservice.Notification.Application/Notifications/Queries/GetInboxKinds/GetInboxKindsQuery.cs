namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetInboxKinds;

/// <summary>
/// The distinct notice kinds in the reader's inbox, to fill the inbox "kind" filter.
/// </summary>
/// <remarks>
/// It takes neither user nor tenant: both come from the <c>IUserContext</c>, as in the inbox itself. The filter offers only
/// what the reader actually has, not every kind the platform knows (pendings/718).
/// </remarks>
public record GetInboxKindsQuery : IRequest<string[]>;
