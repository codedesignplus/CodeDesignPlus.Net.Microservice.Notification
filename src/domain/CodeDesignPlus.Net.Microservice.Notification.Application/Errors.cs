namespace CodeDesignPlus.Net.Microservice.Notification.Application;

public class Errors: IErrorCodes
{
    public const string UnknownError = "200 : UnknownError";
    public const string NotificationNotFound = "402 : The notification was not found.";
}
