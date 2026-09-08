using CodeDesignPlus.Net.Microservice.Notification.Domain.Constants;
using CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;

namespace CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Test.Services;

/// <summary>
/// Cubre a quien alcanza cada forma de empujar por SignalR.
/// </summary>
/// <remarks>
/// El broadcast usaba <c>Clients.All</c>. El comando llevaba el tenant, el agregado lo persistia, y
/// <c>MainHub</c> mete cada conexion en el grupo <c>Tenant:{tenant}</c> al conectar — pero ese grupo no se
/// usaba como destino **nunca**. Resultado: cada pqrs, cada factura y cada votacion de asamblea llegaba a
/// los navegadores de **todas** las copropiedades, no solo a la que la origino.
/// <para>
/// Hoy la fuga esta acotada porque el frontend solo abre el socket en 16 pantallas. En cuanto la conexion
/// pase a ser global —que es lo que esta fase monta— cada residente recibiria el trafico de todos los
/// conjuntos. Por eso esto se arregla antes, y no despues.
/// </para>
/// </remarks>
public class SignalRNotifierAdapterTest
{
    private static readonly Guid Tenant = Guid.Parse("aa000000-0000-4000-8000-000000000001");
    private static readonly Guid OtroTenant = Guid.Parse("bb000000-0000-4000-8000-000000000002");
    private static readonly Guid UserId = Guid.Parse("cc000000-0000-4000-8000-000000000003");

    /// <summary>Publica porque Moq necesita generar un proxy de <c>IHubContext&lt;FakeHub&gt;</c>.</summary>
    public sealed class FakeHub : Hub { }

    private readonly Mock<IHubClients> clients = new();
    private readonly Mock<IClientProxy> proxy = new();

    public SignalRNotifierAdapterTest()
    {
        clients.Setup(x => x.Group(It.IsAny<string>())).Returns(proxy.Object);
        clients.Setup(x => x.User(It.IsAny<string>())).Returns(proxy.Object);
        clients.SetupGet(x => x.All).Returns(proxy.Object);
    }

    [Fact]
    public async Task ABroadcastGoesToTheTenantGroup()
    {
        await BuildAdapter().BroadcastAsync(Tenant, "pqrs.created", "{}", CancellationToken.None);

        clients.Verify(x => x.Group($"{GroupConstants.TenantGroupPrefix}:{Tenant}"), Times.Once);
    }

    [Fact]
    public async Task ABroadcastNeverGoesToEveryone()
    {
        // La asercion que importa: mientras exista un solo Clients.All, la fuga sigue abierta.
        await BuildAdapter().BroadcastAsync(Tenant, "pqrs.created", "{}", CancellationToken.None);

        clients.VerifyGet(x => x.All, Times.Never);
    }

    [Fact]
    public async Task TwoTenantsGetTwoDifferentGroups()
    {
        var adapter = BuildAdapter();

        await adapter.BroadcastAsync(Tenant, "pqrs.created", "{}", CancellationToken.None);
        await adapter.BroadcastAsync(OtroTenant, "pqrs.created", "{}", CancellationToken.None);

        clients.Verify(x => x.Group($"{GroupConstants.TenantGroupPrefix}:{Tenant}"), Times.Once);
        clients.Verify(x => x.Group($"{GroupConstants.TenantGroupPrefix}:{OtroTenant}"), Times.Once);
    }

    [Fact]
    public async Task AGroupPushUsesTheNameItReceives()
    {
        // El calificado con el tenant lo compone quien llama, que es donde vive GroupConstants.
        await BuildAdapter().SendToGroupAsync($"{GroupConstants.TenantGroupPrefix}:{Tenant}:assembly-42", "FloorGranted", "{}", CancellationToken.None);

        clients.Verify(x => x.Group($"{GroupConstants.TenantGroupPrefix}:{Tenant}:assembly-42"), Times.Once);
        clients.VerifyGet(x => x.All, Times.Never);
    }

    [Fact]
    public async Task AUserPushGoesToThatUserOnly()
    {
        await BuildAdapter().SendToUserAsync(UserId, "charge.generation.progress", "{}", CancellationToken.None);

        clients.Verify(x => x.User(UserId.ToString()), Times.Once);
        clients.VerifyGet(x => x.All, Times.Never);
    }

    private SignalRNotifierAdapter<FakeHub> BuildAdapter()
    {
        var hubContext = new Mock<IHubContext<FakeHub>>();
        hubContext.SetupGet(x => x.Clients).Returns(clients.Object);

        return new SignalRNotifierAdapter<FakeHub>(hubContext.Object);
    }
}
