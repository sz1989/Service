namespace Service.Services;

public class AppBackgroundService(ILogger<AppBackgroundService> logger, IBackgroundTaskQueue taskQueue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var (workItem, scopeFactory) = await taskQueue.DequeueAsync(stoppingToken);

            try
            {
                // simulate a deploy call over the network until the real deploy logic exists.
                logger.LogInformation("Simulating deploy network latency (30s)...");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                if (scopeFactory is not null)
                {
                    using var scope = scopeFactory.CreateScope();
                    await workItem(scope.ServiceProvider, stoppingToken);
                }
                else
                {
                    await workItem(null, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing background work item");
            }
        }
    }
}
