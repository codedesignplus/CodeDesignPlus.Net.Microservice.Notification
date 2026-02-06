namespace CodeDesignPlus.Net.Microservice.Notification.Application.Notifications.Queries.GetAllNotifications;

public class GetAllNotificationsQueryHandler(INotificationsRepository repository, IMapper mapper, IUserContext user) : IRequestHandler<GetAllNotificationsQuery, NotificationsDto>
{
    public Task<NotificationsDto> Handle(GetAllNotificationsQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult<NotificationsDto>(default!);
    }
}
