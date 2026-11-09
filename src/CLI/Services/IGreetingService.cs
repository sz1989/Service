namespace CLI.Services;

internal interface IGreetingService
{
    /// <summary>Builds the greeting for <paramref name="name"/>; throws <see cref="GreetingException"/> if it can't.</summary>
    Task<string> GreetAsync(string name, CancellationToken ct);
}
