using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.DataTransferObjects;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetInbox;

/// <summary>
/// La bandeja del lector: lo que le alcanza, de lo mas reciente a lo mas antiguo.
/// </summary>
/// <remarks>
/// No lleva ni usuario ni tenant: salen del <c>IUserContext</c>. Aceptarlos por parametro dejaria leer
/// la bandeja de otro con solo cambiar un numero en la URL.
/// </remarks>
/// <param name="Page">Pagina, empezando en cero.</param>
/// <param name="Size">Cuantos avisos por pagina.</param>
public record GetInboxQuery(int Page, int Size) : IRequest<List<NotificationDto>>;

/// <summary>Validador de <see cref="GetInboxQuery"/>.</summary>
public class Validator : AbstractValidator<GetInboxQuery>
{
    /// <summary>Inicializa las reglas de validacion.</summary>
    public Validator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
        // Con techo: sin el, un `size=100000` obligaria a traerse la coleccion entera a memoria.
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
