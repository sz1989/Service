namespace Service.Services;

public class AppBackgroundService(ILogger<AppBackgroundService> logger, IBackgroundTaskQueue taskQueue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await taskQueue.DequeueAsync(stoppingToken);

            try
            {
                // simulate a deploy call over the network until the real deploy logic exists.
                logger.LogInformation("Simulating deploy network latency (30s)...");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing background work item");
            }
        }
    }
}
