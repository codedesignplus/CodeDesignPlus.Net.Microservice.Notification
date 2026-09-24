using CodeDesignPlus.Net.Exceptions;

namespace CodeDesignPlus.Net.Microservice.Notification.Application;

public class Errors: IErrorCodes
{
    public static readonly Error UnknownError = new("200");
    public static readonly Error NotificationNotFound = new("402");

    /// <summary>El contenido debe ser un JSON válido.</summary>
    public static readonly Error PayloadIsNotValidJson = new("201");

    /// <summary>Un aviso en vivo se dirige a un usuario o a un grupo: no a los dos, ni a ninguno.</summary>
    public static readonly Error PushTargetIsAmbiguous = new("202");
}
