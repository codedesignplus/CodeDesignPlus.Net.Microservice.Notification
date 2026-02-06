namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetNotificationsById;

public class GetNotificationsByIdQueryHandler(INotificationsRepository repository, IMapper mapper, IUserContext user) : IRequestHandler<GetNotificationsByIdQuery, NotificationsDto>
{
    public Task<NotificationsDto> Handle(GetNotificationsByIdQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult<NotificationsDto>(default!);
    }
}
