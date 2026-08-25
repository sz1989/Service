using System.Threading.Channels;

namespace Service.Services;

public class BackgroundTaskQueue(int capacity) : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, Task>> _queue = Channel.CreateBounded<Func<CancellationToken, Task>>(capacity);

    public async Task QueueBackgroundWorkItemAsync(Func<CancellationToken, Task> workItem) =>
        await _queue.Writer.WriteAsync(workItem);

    public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken) =>
        await _queue.Reader.ReadAsync(cancellationToken);
}
