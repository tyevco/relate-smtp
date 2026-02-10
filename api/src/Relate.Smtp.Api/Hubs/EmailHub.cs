using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Relate.Smtp.Api.Hubs;

/// <summary>
/// SignalR hub for real-time email notifications.
/// Clients connect and are automatically added to a group named after their user ID.
/// </summary>
[Authorize]
public class EmailHub : Hub
{
    private readonly ILogger<EmailHub> _logger;

    public EmailHub(ILogger<EmailHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        // Get user ID from claims (set by JWT authentication)
        var userId = Context.User?.FindFirst("sub")?.Value
                     ?? Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Add connection to user-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogDebug("User {UserId} connected to SignalR hub, ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
        }
        else
        {
            _logger.LogWarning("SignalR connection {ConnectionId} established without user ID. User claims: {Claims}",
                Context.ConnectionId,
                Context.User?.Claims != null ? string.Join(", ", Context.User.Claims.Select(c => $"{c.Type}={c.Value}")) : "none");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("sub")?.Value
                     ?? Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogDebug("User {UserId} disconnected from SignalR hub, ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
