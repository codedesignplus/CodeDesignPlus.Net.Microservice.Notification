using CodeDesignPlus.Net.Microservice.Notification.Domain;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Repositories;
using CodeDesignPlus.Net.Microservice.Notification.gRpc.DomainEvents;
using CodeDesignPlus.Net.PubSub.Abstractions;
using CodeDesignPlus.Net.PubSub.Abstractions.Attributes;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Consumers;

/// <summary>
/// Al purgarse una copropiedad, borra sus notificaciones y sus marcas de lectura.
/// </summary>
/// <remarks>
/// Lo publica ms-tenants cuando vence el plazo para restaurar una copropiedad eliminada, y puede llegar más de una
/// vez: borrar lo que ya no está es inofensivo.
/// <para>
/// Vive en el entrypoint gRPC porque este micro no tiene AsyncWorker: el SDK registra los consumidores que
/// encuentra en los ensamblados cargados, y el gRPC ya es donde corre el trabajo de fondo del micro. Crear un
/// AsyncWorker solo para esto obligaría a un despliegue nuevo.
/// </para>
/// <para>
/// <c>PurgeTenantDataHandlerTest</c> recorre el dominio y exige que se purgue todo tipo con <c>Tenant</c>: un
/// agregado nuevo que no se añada aquí hace fallar la prueba (regla 47).
/// </para>
/// </remarks>
[QueueName<NotificationsAggregate>("PurgeTenantDataHandler")]
public class PurgeTenantDataHandler(INotificationsRepository repository, ILogger<PurgeTenantDataHandler> logger) : IEventHandler<TenantPurgedDomainEvent>
{
    public async Task HandleAsync(TenantPurgedDomainEvent data, CancellationToken token)
    {
        var notifications = await repository.DeleteByTenantAsync<NotificationsAggregate>(data.AggregateId, token);
        var reads = await repository.DeleteByTenantAsync<NotificationReadAggregate>(data.AggregateId, token);

        logger.LogInformation("Tenant {TenantId} purged: {Notifications} notifications and {Reads} reads deleted", data.AggregateId, notifications, reads);
    }
}
