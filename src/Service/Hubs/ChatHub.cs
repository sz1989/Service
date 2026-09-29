using Microsoft.AspNetCore.SignalR;

namespace Service.Hubs;

[Authorize]
public class ChatHub(ILogger<ChatHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            logger.LogError(exception, "Client disconnected with an error: {ConnectionId}", Context.ConnectionId);
        }
        else if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}

// https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation