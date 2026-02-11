using System;
using CodeDesignPlus.Net.Security.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Services;

public class UserIdProvider(ILogger<UserIdProvider> logger) : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        logger.LogWarning("Getting user ID for connection {ConnectionId}", connection.ConnectionId);
        logger.LogWarning("User ID for connection {ConnectionId} is {UserId}", connection.ConnectionId, connection.User?.FindFirst(ClaimTypes.UserId)?.Value);

        return connection.User?.FindFirst(ClaimTypes.UserId)?.Value;
    }
}