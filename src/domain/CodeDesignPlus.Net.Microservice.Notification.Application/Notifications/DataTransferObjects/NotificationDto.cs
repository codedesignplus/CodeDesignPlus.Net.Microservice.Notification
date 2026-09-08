namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.DataTransferObjects;

/// <summary>
/// Un aviso, tal y como lo lee la bandeja.
/// </summary>
/// <remarks>
/// La <c>Audience</c> <b>no se expone</b>: es como se enruta el aviso, no algo que el lector necesite
/// saber. Devolverla contaria de paso quien mas lo recibio.
/// </remarks>
/// <param name="Id">El aviso.</param>
/// <param name="Kind">La clave estable del tipo; el frontend la traduce a texto e icono.</param>
/// <param name="Title">Respaldo para cuando el frontend no conoce el <paramref name="Kind"/>.</param>
/// <param name="Body">El texto del aviso.</param>
/// <param name="Resource">A donde lleva el clic, o <c>null</c> si no lleva a ninguna parte.</param>
/// <param name="PayloadPreview">El contenido serializado que acompaña al aviso.</param>
/// <param name="OccurredAt">Cuando ocurrio el hecho, que no es cuando se guardo.</param>
/// <param name="IsRead">Si este lector ya lo leyo.</param>
public record NotificationDto(
    Guid Id,
    string Kind,
    string Title,
    string Body,
    ResourceDto? Resource,
    string? PayloadPreview,
    Instant OccurredAt,
    bool IsRead);

/// <summary>
/// El recurso al que lleva el clic.
/// </summary>
/// <remarks>
/// Modulo e identificador, nunca una ruta: las rutas son del frontend y cambian sin que el backend se
/// entere.
/// </remarks>
/// <param name="Module">El modulo dueño del dato, ej. <c>invoicing</c>.</param>
/// <param name="AggregateId">El identificador del agregado dentro de ese modulo.</param>
public record ResourceDto(string Module, string AggregateId);
