namespace CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Repositories;

public class NotificationsRepository(IServiceProvider serviceProvider, IOptions<MongoOptions> mongoOptions, ILogger<NotificationsRepository> logger) 
    : RepositoryBase(serviceProvider, mongoOptions, logger), INotificationsRepository
{
   
}