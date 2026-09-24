using CodeDesignPlus.Net.Exceptions;

namespace CodeDesignPlus.Net.Microservice.Notification.Domain;

public class Errors: IErrorCodes
{
    public static readonly Error UnknownError = new("100");
    public static readonly Error NotificationAlreadyDelivered = new("401");
    public static readonly Error NotificationNotFound = new("400");

    /// <summary>Sin kind la bandeja no sabe que texto ni que icono pintar.</summary>
    public static readonly Error NotificationKindIsRequired = new("403");

    /// <summary>El titulo es el respaldo cuando el frontend no conoce el kind.</summary>
    public static readonly Error NotificationTitleIsRequired = new("404");

    /// <summary>Un acuse de lectura sin aviso no identifica nada.</summary>
    public static readonly Error NotificationIdIsRequired = new("405");

    /// <summary>Un acuse de lectura sin lector no distingue quien leyo.</summary>
    public static readonly Error ReaderIsRequired = new("406");
}
