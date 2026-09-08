using CodeDesignPlus.Net.Microservice.Notification.Domain.Constants;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.PushLive;

/// <summary>
/// Manejador de <see cref="PushLiveCommand"/>.
/// </summary>
/// <remarks>
/// No toca el repositorio a proposito: un push perdido es un push perdido y no hay nada que reintentar
/// ni que guardar. Si el mensaje merece sobrevivir a que nadie estuviera mirando, no es un push: es un
/// aviso, y va por <see cref="Notify.NotifyCommand"/>.
/// </remarks>
public class PushLiveCommandHandler(INotifierGateway notifier) : IRequestHandler<PushLiveCommand, bool>
{
    /// <summary>Empuja el mensaje al destinatario indicado.</summary>
    public async Task<bool> Handle(PushLiveCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.UserId.HasValue)
                await notifier.SendToUserAsync(request.UserId.Value, request.EventName, request.JsonPayload, cancellationToken);
            else
                await notifier.SendToGroupAsync(
                    GroupConstants.BuildTenantGroupName(request.Tenant, request.GroupName!),
                    request.EventName, request.JsonPayload, cancellationToken);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
