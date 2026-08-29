namespace Service.Services;

public interface IBackgroundTaskQueue
{
    Task QueueBackgroundWorkItemAsync(
        Func<IServiceProvider?, CancellationToken, Task> workItem,
        IServiceScopeFactory? scopeFactory = null);

    Task<(Func<IServiceProvider?, CancellationToken, Task> WorkItem, IServiceScopeFactory? ScopeFactory)> DequeueAsync(CancellationToken cancellationToken);
}
