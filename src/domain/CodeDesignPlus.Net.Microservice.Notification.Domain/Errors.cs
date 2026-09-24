using CodeDesignPlus.Net.Exceptions;

namespace CodeDesignPlus.Net.Microservice.Notification.Domain;

public class Errors: IErrorCodes
{
    public static readonly Error UnknownError = new("100", "UnknownError");
    public static readonly Error NotificationAlreadyDelivered = new("401", "The notification was already delivered.");
    public static readonly Error NotificationNotFound = new("400", "The notification was not found.");

    /// <summary>Sin kind la bandeja no sabe que texto ni que icono pintar.</summary>
    public static readonly Error NotificationKindIsRequired = new("403", "The notification kind is required.");

    /// <summary>El titulo es el respaldo cuando el frontend no conoce el kind.</summary>
    public static readonly Error NotificationTitleIsRequired = new("404", "The notification title is required.");

    /// <summary>Un acuse de lectura sin aviso no identifica nada.</summary>
    public static readonly Error NotificationIdIsRequired = new("405", "The notification id is required.");

    /// <summary>Un acuse de lectura sin lector no distingue quien leyo.</summary>
    public static readonly Error ReaderIsRequired = new("406", "The reader is required.");
}
