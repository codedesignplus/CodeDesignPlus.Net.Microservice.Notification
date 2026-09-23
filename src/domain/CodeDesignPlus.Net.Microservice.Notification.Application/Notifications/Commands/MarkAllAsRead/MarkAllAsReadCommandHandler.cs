namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.MarkAllAsRead;

/// <summary>
/// Manejador de <see cref="MarkAllAsReadCommand"/>.
/// </summary>
/// <remarks>
/// Recorre la bandeja por paginas y acusa una a una. No es elegante, pero es lo unico honesto con el
/// modelo: el acuse es por (aviso, lector), asi que "todo leido" son tantas filas como avisos le
/// alcancen. La alternativa —una marca de agua por fecha— haria que un aviso viejo que llegara tarde
/// naciera ya leido.
/// <para>
/// El techo de paginas evita que un usuario con años de bandeja bloquee la peticion; lo que quede sin
/// acusar se acusa en la siguiente pasada.
/// </para>
/// </remarks>
public class MarkAllAsReadCommandHandler(
    INotificationsRepository repository,
    INotificationReadRepository readRepository,
    IUserContext user,
    IRoleDirectory roleDirectory) : IRequestHandler<MarkAllAsReadCommand>
{
    private const int TamanoDePagina = 100;
    private const int MaximoDePaginas = 20;

    /// <summary>Acusa todo lo que le alcanza al lector.</summary>
    public async Task Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        // Se resuelven una vez y no por pagina: son los mismos durante todo el recorrido, y pedirlos en
        // cada vuelta convertiria un acuse masivo en veinte consultas al directorio.
        var roles = await roleDirectory.GetRolesAsync(user.IdUser, user.Tenant, cancellationToken);

        for (var pagina = 0; pagina < MaximoDePaginas; pagina++)
        {
            var criteria = new C.Criteria { Skip = pagina * TamanoDePagina, Limit = TamanoDePagina };

            // Sin `excluir`: la bandeja no encoge al acusar, asi que el salto por paginas es estable.
            // Pasar los ya leidos moveria las filas bajo los pies del recorrido.
            var avisos = (await repository.GetInboxAsync(user.Tenant, user.IdUser, roles, criteria, null, cancellationToken)).Data.ToList();

            if (avisos.Count == 0)
                return;

            var yaLeidos = await readRepository.GetReadIdsAsync(user.Tenant, user.IdUser, avisos.Select(x => x.Id), cancellationToken);

            foreach (var aviso in avisos.Where(x => !yaLeidos.Contains(x.Id)))
            {
                await readRepository.MarkAsReadAsync(
                    NotificationReadAggregate.Create(aviso.Id, user.IdUser, user.Tenant),
                    cancellationToken);
            }

            if (avisos.Count < TamanoDePagina)
                return;
        }
    }
}
