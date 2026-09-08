namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.MarkAsRead;

/// <summary>
/// Manejador de <see cref="MarkAsReadCommand"/>.
/// </summary>
/// <remarks>
/// No comprueba que el aviso le alcance al lector, y es deliberado: un acuse sobre un aviso ajeno no
/// revela nada —no devuelve su contenido— y la comprobacion costaria una lectura extra en el camino mas
/// frecuente de la campana. Lo que si lleva es el tenant, para que el acuse no cruce copropiedades.
/// </remarks>
public class MarkAsReadCommandHandler(INotificationReadRepository repository, IUserContext user) : IRequestHandler<MarkAsReadCommand>
{
    /// <summary>Registra el acuse.</summary>
    public Task Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
        => repository.MarkAsReadAsync(
            NotificationReadAggregate.Create(request.NotificationId, user.IdUser, user.Tenant),
            cancellationToken);
}
