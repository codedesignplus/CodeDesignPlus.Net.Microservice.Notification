namespace CodeDesignPlus.Net.Microservice.Notification.Domain;

public class Errors: IErrorCodes
{
    public const string UnknownError = "100 : UnknownError";
    public const string NotificationAlreadyDelivered = "401 : The notification was already delivered.";
    public const string NotificationNotFound = "402 : The notification was not found.";

    /// <summary>Sin kind la bandeja no sabe que texto ni que icono pintar.</summary>
    public const string NotificationKindIsRequired = "403 : The notification kind is required.";

    /// <summary>El titulo es el respaldo cuando el frontend no conoce el kind.</summary>
    public const string NotificationTitleIsRequired = "404 : The notification title is required.";

    /// <summary>Un acuse de lectura sin aviso no identifica nada.</summary>
    public const string NotificationIdIsRequired = "405 : The notification id is required.";

    /// <summary>Un acuse de lectura sin lector no distingue quien leyo.</summary>
    public const string ReaderIsRequired = "406 : The reader is required.";
}
