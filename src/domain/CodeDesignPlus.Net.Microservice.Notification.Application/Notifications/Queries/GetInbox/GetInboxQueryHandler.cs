using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.DataTransferObjects;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetInbox;

/// <summary>
/// Manejador de <see cref="GetInboxQuery"/>.
/// </summary>
/// <remarks>
/// La audiencia se resuelve aqui, al leer, contra el <c>IUserContext</c>: el lector trae sus roles en su
/// propio JWT, asi que sale gratis. Resolverla al escribir habria obligado a que cada micro supiera los
/// identificadores de usuario detras de un rol.
/// </remarks>
public class GetInboxQueryHandler(
    INotificationsRepository repository,
    INotificationReadRepository readRepository,
    IUserContext user) : IRequestHandler<GetInboxQuery, List<NotificationDto>>
{
    /// <summary>Trae una pagina de la bandeja del lector.</summary>
    public async Task<List<NotificationDto>> Handle(GetInboxQuery request, CancellationToken cancellationToken)
    {
        var avisos = await repository.GetInboxAsync(user.Tenant, user.IdUser, user.Roles, request.Page, request.Size, cancellationToken);

        if (avisos.Count == 0)
            return [];

        // Se pregunta por los acuses de esta pagina, no por todos los del usuario: una bandeja de dos
        // años no cabe en memoria y no hace falta para pintar veinte filas.
        var leidos = await readRepository.GetReadIdsAsync(user.Tenant, user.IdUser, avisos.Select(x => x.Id), cancellationToken);

        return [.. avisos.Select(x => new NotificationDto(
            x.Id,
            x.Kind ?? string.Empty,
            x.Title ?? string.Empty,
            x.Body ?? string.Empty,
            x.Resource is null ? null : new ResourceDto(x.Resource.Module, x.Resource.AggregateId),
            x.PayloadPreview,
            x.OccurredAt,
            leidos.Contains(x.Id)))];
    }
}
