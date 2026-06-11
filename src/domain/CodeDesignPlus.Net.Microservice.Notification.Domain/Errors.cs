namespace CodeDesignPlus.Net.Microservice.Notification.Domain;

public class Errors: IErrorCodes
{
    public const string UnknownError = "100 : UnknownError";
    public const string NotificationAlreadyDelivered = "401 : The notification was already delivered.";
    public const string NotificationNotFound = "402 : The notification was not found.";
}
