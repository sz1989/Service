namespace CLI.Services;

internal sealed class GreetingService : IGreetingService
{
    public Task<string> GreetAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new GreetingException("A name is required to greet someone.");
        }

        return Task.FromResult($"Hello, {name.Trim()}!");
    }
}
