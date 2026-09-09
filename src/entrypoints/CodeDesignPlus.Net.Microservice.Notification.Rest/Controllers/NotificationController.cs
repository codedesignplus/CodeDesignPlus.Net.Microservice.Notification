using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.MarkAllAsRead;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Commands.MarkAsRead;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.DataTransferObjects;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetInbox;
using CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetUnreadCount;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using CodeDesignPlus.Net.Core.Abstractions.Models.Pager;
using System.ComponentModel;

namespace CodeDesignPlus.Net.Microservice.Notification.Rest.Controllers;

/// <summary>
/// La bandeja de avisos del usuario que llama.
/// </summary>
/// <remarks>
/// Ningun endpoint recibe el usuario ni la copropiedad: los dos salen del token. Aceptarlos por
/// parametro dejaria leer la bandeja de otro cambiando un identificador en la URL.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class NotificationController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Los avisos que le alcanzan al usuario, del mas reciente al mas antiguo.
    /// </summary>
    /// <remarks>
    /// Es un listado de criteria como cualquier otro de la plataforma, asi que la tabla del frontend se
    /// escribe igual que las demas. Lo que llegue en <paramref name="criteria"/> solo puede estrechar la
    /// bandeja: la audiencia se aplica antes y por dentro, no se puede ensanchar desde la URL.
    /// <para>
    /// <paramref name="unreadOnly"/> va aparte porque los acuses de lectura viven en otra coleccion: no
    /// es un campo del aviso y el parser de criteria no puede resolverlo.
    /// </para>
    /// </remarks>
    /// <param name="criteria">Filtros, orden y pagina.</param>
    /// <param name="unreadOnly">Solo los que aun no ha acusado.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>Una pagina de la bandeja.</returns>
    [HttpGet]
    [Description("Get the caller's notification inbox")]
    [ProducesResponseType(typeof(Pagination<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetInbox(
        [FromQuery] C.Criteria criteria,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new GetInboxQuery(criteria, unreadOnly), cancellationToken));

    /// <summary>
    /// Cuantos avisos sin leer tiene, para el numerito de la campana.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    /// <returns>El conteo de no leidos.</returns>
    [HttpGet("unread-count")]
    [Description("Get the caller's unread notification count")]
    [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetUnreadCountQuery(), cancellationToken));

    /// <summary>
    /// Deja constancia de que el usuario vio un aviso.
    /// </summary>
    /// <param name="id">El aviso leido.</param>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    [HttpPost("{id:guid}/read")]
    [Description("Mark one notification as read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new MarkAsReadCommand(id), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Deja constancia de que el usuario vio todo lo pendiente.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelacion.</param>
    [HttpPost("read-all")]
    [Description("Mark every notification as read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await mediator.Send(new MarkAllAsReadCommand(), cancellationToken);

        return NoContent();
    }
}
