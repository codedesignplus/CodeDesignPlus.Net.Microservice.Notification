using CodeDesignPlus.Net.Exceptions;

namespace CodeDesignPlus.Net.Microservice.Notification.Application;

public class Errors: IErrorCodes
{
    public static readonly Error UnknownError = new("200", "UnknownError");
    public static readonly Error NotificationNotFound = new("402", "The notification was not found.");
}
