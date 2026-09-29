using System.Threading.Channels;

namespace Service.Services;

public class ChatRequestQueue : IChatRequestQueue
{
    private readonly Channel<ChatAskWorkItem> _queue = Channel.CreateUnbounded<ChatAskWorkItem>();

    public async Task QueueAsync(ChatAskWorkItem item) => await _queue.Writer.WriteAsync(item);

    public async Task<ChatAskWorkItem> DequeueAsync(CancellationToken cancellationToken) => await _queue.Reader.ReadAsync(cancellationToken);
}
