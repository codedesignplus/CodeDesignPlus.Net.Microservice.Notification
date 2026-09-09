using CodeDesignPlus.Net.Core.Abstractions.Models.Pager;
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
    IUserContext user) : IRequestHandler<GetInboxQuery, Pagination<NotificationDto>>
{
    /// <summary>Trae una pagina de la bandeja del lector.</summary>
    public async Task<Pagination<NotificationDto>> Handle(GetInboxQuery request, CancellationToken cancellationToken)
    {
        // Con "solo sin leer" los acuses se traen enteros y se descartan en la consulta. Filtrarlos
        // despues de paginar dejaria el total contando filas que el usuario no va a ver, y la tabla
        // anunciando paginas vacias.
        var yaLeidos = request.UnreadOnly
            ? await readRepository.GetAllReadIdsAsync(user.Tenant, user.IdUser, cancellationToken)
            : null;

        var pagina = await repository.GetInboxAsync(user.Tenant, user.IdUser, user.Roles, request.Criteria, yaLeidos, cancellationToken);

        var avisos = pagina.Data.ToList();

        if (avisos.Count == 0)
            return Pagination<NotificationDto>.Create([], pagina.TotalCount, pagina.Limit, pagina.Skip);

        // Se pregunta por los acuses de esta pagina, no por todos los del usuario: una bandeja de dos
        // años no cabe en memoria y no hace falta para pintar veinte filas. Con "solo sin leer" ya se
        // sabe la respuesta: ninguno esta leido, porque justo eso es lo que se descarto en la consulta.
        HashSet<Guid> leidos = request.UnreadOnly
            ? []
            : await readRepository.GetReadIdsAsync(user.Tenant, user.IdUser, avisos.Select(x => x.Id), cancellationToken);

        var data = avisos.Select(x => new NotificationDto(
            x.Id,
            x.Kind ?? string.Empty,
            x.Title ?? string.Empty,
            x.Body ?? string.Empty,
            x.Resource is null ? null : new ResourceDto(x.Resource.Module, x.Resource.AggregateId),
            x.PayloadPreview,
            x.OccurredAt,
            leidos.Contains(x.Id)));

        return Pagination<NotificationDto>.Create(data, pagina.TotalCount, pagina.Limit, pagina.Skip);
    }
}
