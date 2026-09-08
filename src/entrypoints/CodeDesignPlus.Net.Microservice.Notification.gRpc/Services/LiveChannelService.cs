using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.PushLive;

namespace CodeDesignPlus.Net.Microservice.Notification.gRpc.Services;

/// <summary>
/// Punto de entrada del canal efimero.
/// </summary>
/// <remarks>
/// No persiste nada y no debe hacerlo. Si el mensaje merece sobrevivir a que el destinatario no
/// estuviera mirando, no va por aqui: va por <see cref="InboxService"/>.
/// </remarks>
public class LiveChannelService(IMediator mediator) : LiveChannel.LiveChannelBase
{
    /// <summary>Empuja mensajes efimeros a un usuario concreto.</summary>
    public override Task PushToUser(IAsyncStreamReader<LiveUserPush> requestStream, IServerStreamWriter<PushAck> responseStream, ServerCallContext context)
        => PumpAsync(requestStream, responseStream, context, request =>
        {
            var tenant = ParseGuid(request.Tenant, nameof(request.Tenant));
            var userId = ParseGuid(request.UserId, nameof(request.UserId));

            return new PushLiveCommand(tenant, userId, null, request.EventName, request.JsonPayload);
        });

    /// <summary>Empuja mensajes efimeros a un grupo de la copropiedad.</summary>
    public override Task PushToGroup(IAsyncStreamReader<LiveGroupPush> requestStream, IServerStreamWriter<PushAck> responseStream, ServerCallContext context)
        => PumpAsync(requestStream, responseStream, context, request =>
        {
            var tenant = ParseGuid(request.Tenant, nameof(request.Tenant));

            return new PushLiveCommand(tenant, null, request.GroupName, request.EventName, request.JsonPayload);
        });

    /// <summary>
    /// Lee el stream, manda cada mensaje por MediatR y escribe un acuse por cada uno.
    /// </summary>
    /// <remarks>
    /// El <c>try</c> va dentro del bucle a proposito: un mensaje con un tenant mal formado no puede tumbar
    /// el stream, que es de larga duracion y lo comparten todas las peticiones del emisor.
    /// </remarks>
    private async Task PumpAsync<TRequest>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<PushAck> responseStream,
        ServerCallContext context,
        Func<TRequest, PushLiveCommand> toCommand)
    {
        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            try
            {
                var result = await mediator.Send(toCommand(request), context.CancellationToken);

                await responseStream.WriteAsync(new PushAck { Success = result, Message = result ? "Pushed" : "Not delivered" });
            }
            catch (Exception ex)
            {
                await responseStream.WriteAsync(new PushAck { Success = false, Message = ex.Message });
            }
        }
    }

    private static Guid ParseGuid(string value, string field)
        => Guid.TryParse(value, out var parsed) ? parsed : throw new InvalidCastException($"Invalid {field} format: {value}");
}
