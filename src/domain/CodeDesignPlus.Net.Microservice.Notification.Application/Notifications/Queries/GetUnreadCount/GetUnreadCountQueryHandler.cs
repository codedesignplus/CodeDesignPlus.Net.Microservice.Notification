namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetUnreadCount;

/// <summary>
/// Manejador de <see cref="GetUnreadCountQuery"/>.
/// </summary>
/// <remarks>
/// Los avisos y los acuses viven en colecciones distintas a proposito: el acuse de un aviso de tenant lo
/// escribiria cada usuario de la copropiedad, y como array dentro del aviso no tendria techo. Por eso se traen
/// los acuses del lector y se descartan en la consulta de los avisos abiertos, la misma que usa la bandeja con
/// "solo sin leer" (pendings/325).
/// <para>
/// El resultado <b>no se emite como metrica por kind</b>: eso multiplicaria las series en SigNoz
/// (regla 13, seccion 6).
/// </para>
/// </remarks>
public class GetUnreadCountQueryHandler(
    INotificationsRepository repository,
    INotificationReadRepository readRepository,
    IUserContext user,
    IRoleDirectory roleDirectory) : IRequestHandler<GetUnreadCountQuery, long>
{
    /// <summary>Cuenta lo que le alcanza al lector y aun no ha acusado.</summary>
    public async Task<long> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        // Los roles de esta copropiedad, no los del claim: el contador tiene que contar exactamente lo
        // mismo que la bandeja, o la campana anuncia avisos que no aparecen al abrirla.
        var roles = await roleDirectory.GetRolesAsync(user.IdUser, user.Tenant, cancellationToken);

        // Lo que alcanza, sin acuse y sin cerrar, en una sola consulta. Restar los acuses de lo que alcanza dejaba de cuadrar
        // en cuanto un aviso se cierra: uno cerrado y leido se restaria dos veces (pendings/325).
        var leidos = await readRepository.GetAllReadIdsAsync(user.Tenant, user.IdUser, cancellationToken);

        return await repository.CountPendingAsync(user.Tenant, user.IdUser, roles, leidos, cancellationToken);
    }
}
