using CodeDesignPlus.Net.Core.Abstractions.Models.Pager;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.DataTransferObjects;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetInbox;

/// <summary>
/// La bandeja del lector: lo que le alcanza, de lo mas reciente a lo mas antiguo.
/// </summary>
/// <remarks>
/// No lleva ni usuario ni tenant: salen del <c>IUserContext</c>. Aceptarlos por parametro dejaria leer
/// la bandeja de otro con solo cambiar un numero en la URL.
/// <para>
/// <paramref name="UnreadOnly"/> va aparte del <c>criteria</c> y no como un filtro mas porque los acuses
/// de lectura viven en <b>otra coleccion</b>: no es un campo del aviso y el parser de criteria no puede
/// resolverlo.
/// </para>
/// </remarks>
/// <param name="Criteria">Filtros, orden y pagina, como en cualquier listado de la plataforma.</param>
/// <param name="UnreadOnly">Solo los que aun no ha acusado.</param>
public record GetInboxQuery(C.Criteria Criteria, bool UnreadOnly = false) : IRequest<Pagination<NotificationDto>>;

/// <summary>Validador de <see cref="GetInboxQuery"/>.</summary>
public class Validator : AbstractValidator<GetInboxQuery>
{
    /// <summary>Inicializa las reglas de validacion.</summary>
    public Validator()
    {
        RuleFor(x => x.Criteria).NotNull();
        RuleFor(x => x.Criteria.Skip).GreaterThanOrEqualTo(0).When(x => x.Criteria is not null && x.Criteria.Skip.HasValue);
        // Con techo: sin el, un `limit=100000` obligaria a traerse la coleccion entera a memoria.
        RuleFor(x => x.Criteria.Limit).InclusiveBetween(1, 100).When(x => x.Criteria is not null && x.Criteria.Limit.HasValue);
    }
}
