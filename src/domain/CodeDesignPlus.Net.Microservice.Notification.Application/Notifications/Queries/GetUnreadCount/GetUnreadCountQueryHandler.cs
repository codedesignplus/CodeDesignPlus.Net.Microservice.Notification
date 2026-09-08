namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetUnreadCount;

/// <summary>
/// Manejador de <see cref="GetUnreadCountQuery"/>.
/// </summary>
/// <remarks>
/// Son dos conteos —lo que me alcanza y lo que ya acuse— y no una consulta cruzada, porque los avisos y
/// los acuses viven en colecciones distintas a proposito: el acuse de un aviso de tenant lo escribiria
/// cada usuario de la copropiedad, y como array dentro del aviso no tendria techo.
/// <para>
/// El resultado <b>no se emite como metrica por kind</b>: eso multiplicaria las series en SigNoz
/// (regla 13, seccion 6).
/// </para>
/// </remarks>
public class GetUnreadCountQueryHandler(
    INotificationsRepository repository,
    INotificationReadRepository readRepository,
    IUserContext user) : IRequestHandler<GetUnreadCountQuery, long>
{
    /// <summary>Cuenta lo que le alcanza al lector y aun no ha acusado.</summary>
    public async Task<long> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var alcanzan = await repository.CountInboxAsync(user.Tenant, user.IdUser, user.Roles, cancellationToken);
        var leidos = await readRepository.CountReadAsync(user.Tenant, user.IdUser, cancellationToken);

        // Nunca negativo: un acuse puede sobrevivir al aviso que lo origino si este se purga.
        return Math.Max(0, alcanzan - leidos);
    }
}
