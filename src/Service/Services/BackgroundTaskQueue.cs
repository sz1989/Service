using System.Threading.Channels;

namespace Service.Services;

public class BackgroundTaskQueue(int capacity) : IBackgroundTaskQueue
{
    private readonly Channel<(Func<IServiceProvider?, CancellationToken, Task> WorkItem, IServiceScopeFactory? ScopeFactory)> _queue =
        Channel.CreateBounded<(Func<IServiceProvider?, CancellationToken, Task>, IServiceScopeFactory?)>(capacity);

    public async Task QueueBackgroundWorkItemAsync(
        Func<IServiceProvider?, CancellationToken, Task> workItem,
        IServiceScopeFactory? scopeFactory = null) =>
        await _queue.Writer.WriteAsync((workItem, scopeFactory));

    public async Task<(Func<IServiceProvider?, CancellationToken, Task> WorkItem, IServiceScopeFactory? ScopeFactory)> DequeueAsync(CancellationToken cancellationToken) =>
        await _queue.Reader.ReadAsync(cancellationToken);
}
