namespace CLI.Services;

internal sealed class GreetingService(ILogger<GreetingService> logger) : IGreetingService
{
    public Task<string> GreetAsync(string name, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new GreetingException("A name is required to greet someone.");
        }

        var trimmed = name.Trim();
        logger.LogInformation("Greeting {Name}", trimmed);

        return Task.FromResult($"Hello, {trimmed}!");
    }
}
