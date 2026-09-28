namespace Service.Services;

public record ChatAskWorkItem(Guid RequestId, string Question, string ConnectionId);

public interface IChatRequestQueue
{
    Task QueueAsync(ChatAskWorkItem item);

    Task<ChatAskWorkItem> DequeueAsync(CancellationToken cancellationToken);
}
