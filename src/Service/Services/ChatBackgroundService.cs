using Microsoft.AspNetCore.SignalR;
using Service.BAL.Chat;
using Service.Hubs;

namespace Service.Services;

public class ChatBackgroundService(
    ILogger<ChatBackgroundService> logger,
    IChatRequestQueue queue,
    IChatRequestRegistry registry,
    IServiceScopeFactory scopeFactory,
    IHubContext<ChatHub> hubContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var item = await queue.DequeueAsync(stoppingToken);
            registry.TryGetToken(item.RequestId, out var requestToken);

            try
            {
                if (requestToken.IsCancellationRequested)
                {
                    logger.LogInformation("Chat request {RequestId} was cancelled before it started", item.RequestId);

                    await hubContext.Clients.Client(item.ConnectionId)
                        .SendAsync("chatCancelled", new ChatCancelledMessage(item.RequestId), stoppingToken);

                    continue;
                }

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, requestToken);
                using var scope = scopeFactory.CreateScope();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
                var answer = await chatService.AskAsync(item.Question, linkedCts.Token);

                logger.LogInformation(
                    "Chat request {RequestId} answered, notifying connection {ConnectionId}",
                    item.RequestId, item.ConnectionId);

                await hubContext.Clients.Client(item.ConnectionId)
                    .SendAsync("chatAnswer", new ChatAnswerMessage(item.RequestId, item.Question, answer), stoppingToken);
            }
            catch (OperationCanceledException) when (requestToken.IsCancellationRequested)
            {
                logger.LogInformation("Chat request {RequestId} was cancelled while in progress", item.RequestId);

                await hubContext.Clients.Client(item.ConnectionId)
                    .SendAsync("chatCancelled", new ChatCancelledMessage(item.RequestId), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error answering chat request {RequestId}", item.RequestId);

                await hubContext.Clients.Client(item.ConnectionId)
                    .SendAsync("chatError", new ChatErrorMessage(item.RequestId, "Failed to get a response."), stoppingToken);
            }
            finally
            {
                registry.Complete(item.RequestId);
            }
        }
    }
}
