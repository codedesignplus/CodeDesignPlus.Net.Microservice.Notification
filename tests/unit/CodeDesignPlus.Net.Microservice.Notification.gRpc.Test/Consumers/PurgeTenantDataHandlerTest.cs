using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeDesignPlus.Net.Core.Abstractions;
using CodeDesignPlus.Net.Microservice.Notification.Domain;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Repositories;
using CodeDesignPlus.Net.Microservice.Notification.gRpc.Consumers;
using CodeDesignPlus.Net.Microservice.Notification.gRpc.DomainEvents;
using CodeDesignPlus.Net.Mongo.Abstractions;
using Microsoft.Extensions.Logging;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Test.Consumers;

/// <summary>
/// Al purgarse una copropiedad no puede quedar en este micro ningún documento suyo (regla 47).
/// </summary>
public class PurgeTenantDataHandlerTest
{
    /// <summary>
    /// Los tipos persistidos del micro que guardan la copropiedad en una propiedad <c>Tenant</c>.
    /// </summary>
    /// <remarks>
    /// Sale del dominio por reflexión y no de una lista escrita a mano: así un agregado nuevo entra solo en la
    /// prueba, y si nadie lo añade al consumidor, la prueba falla.
    /// </remarks>
    private static HashSet<Type> TypesWithTenant() => typeof(NotificationsAggregate).Assembly.GetTypes()
        .Where(type => type.IsClass && !type.IsAbstract && typeof(IEntityBase).IsAssignableFrom(type))
        .Where(type => type.GetProperty("Tenant", BindingFlags.Public | BindingFlags.Instance)?.PropertyType is { } property && (property == typeof(Guid) || property == typeof(Guid?)))
        .ToHashSet();

    [Fact]
    public async Task HandleAsync_TenantPurged_DeletesEveryTypeWithTenant()
    {
        // Arrange
        var repository = new Mock<INotificationsRepository>();
        var tenant = Guid.NewGuid();
        var handler = new PurgeTenantDataHandler(repository.Object, Mock.Of<ILogger<PurgeTenantDataHandler>>());

        // Act
        await handler.HandleAsync(TenantPurgedDomainEvent.Create(tenant, "Malpelo XXI"), CancellationToken.None);

        // Assert
        var purged = repository.Invocations
            .Where(invocation => invocation.Method.Name == nameof(IRepositoryBase.DeleteByTenantAsync) && (Guid)invocation.Arguments[0] == tenant)
            .Select(invocation => invocation.Method.GetGenericArguments()[0])
            .ToHashSet();

        Assert.Empty(TypesWithTenant().Except(purged).Select(type => type.Name));
    }

    [Fact]
    public void TypesWithTenant_Domain_FindsTheAggregates()
    {
        // Act
        var types = TypesWithTenant();

        // Assert
        Assert.Contains(typeof(NotificationsAggregate), types);
        Assert.Contains(typeof(NotificationReadAggregate), types);
    }
}
