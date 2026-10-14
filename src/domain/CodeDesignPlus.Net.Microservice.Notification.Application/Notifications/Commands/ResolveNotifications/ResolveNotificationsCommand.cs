namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.ResolveNotifications;

/// <summary>
/// Cierra los avisos de una tarea que ya se hizo, por su clave de correlacion: el tipo de aviso mas el recurso al que llevan.
/// </summary>
/// <remarks>
/// Es generico a proposito: el servicio no sabe que tarea pedia el aviso ni quien puede hacerla. Lo decide el micro dueño
/// del recurso, que es quien sabe cuando se hizo (pendings/325).
/// </remarks>
/// <param name="Tenant">La copropiedad.</param>
/// <param name="Kinds">Los tipos de aviso que se cierran.</param>
/// <param name="Module">El modulo dueño del recurso.</param>
/// <param name="AggregateId">El recurso.</param>
/// <param name="ResolvedBy">Quien hizo la tarea, o el usuario de sistema.</param>
/// <param name="ResolvedAt">Cuando se hizo: los avisos de hechos posteriores son otra tarea y no se cierran.</param>
public record ResolveNotificationsCommand(Guid Tenant, IReadOnlyList<string> Kinds, string Module, string AggregateId, Guid ResolvedBy, Instant ResolvedAt) : IRequest<long>;

/// <summary>Validador de <see cref="ResolveNotificationsCommand"/>.</summary>
public class Validator : AbstractValidator<ResolveNotificationsCommand>
{
    /// <summary>Inicializa las reglas de validacion.</summary>
    public Validator()
    {
        RuleFor(x => x.Tenant).NotEmpty();
        RuleFor(x => x.Kinds).NotEmpty();
        RuleForEach(x => x.Kinds).NotEmpty();
        RuleFor(x => x.Module).NotEmpty();
        RuleFor(x => x.AggregateId).NotEmpty();
    }
}

/// <summary>
/// Manejador de <see cref="ResolveNotificationsCommand"/>.
/// </summary>
/// <remarks>
/// Primero guarda el cierre de cada tipo y despues cierra los avisos ya guardados. En ese orden, un aviso que se guarde entre
/// los dos pasos tambien queda cerrado: lo cierra la consulta, o nace cerrado al ver el cierre.
/// </remarks>
public class ResolveNotificationsCommandHandler(INotificationsRepository repository) : IRequestHandler<ResolveNotificationsCommand, long>
{
    /// <summary>Guarda los cierres y cierra los avisos abiertos que cubren.</summary>
    /// <returns>Cuantos avisos ya guardados se cerraron.</returns>
    public async Task<long> Handle(ResolveNotificationsCommand request, CancellationToken cancellationToken)
    {
        var kinds = request.Kinds.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.Ordinal).ToList();

        foreach (var kind in kinds)
        {
            var id = NotificationResolutionAggregate.IdFor(request.Tenant, kind, request.Module, request.AggregateId);

            var existing = await repository.FindAsync<NotificationResolutionAggregate>(id, request.Tenant, cancellationToken);

            if (existing is null)
            {
                await repository.CreateAsync(
                    NotificationResolutionAggregate.Create(request.Tenant, kind, request.Module, request.AggregateId, request.ResolvedBy, request.ResolvedAt), cancellationToken);
            }
            else if (existing.Advance(request.ResolvedBy, request.ResolvedAt))
            {
                await repository.UpdateAsync(existing, cancellationToken);
            }
        }

        return await repository.ResolveAsync(request.Tenant, kinds, request.Module, request.AggregateId, request.ResolvedBy, request.ResolvedAt, cancellationToken);
    }
}
