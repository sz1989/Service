using Microsoft.Extensions.DependencyInjection;

namespace CLI.Tests;

/// <summary>Builds the CLI's real command registrations on top of <see cref="IGreetingService"/>/<see cref="IPeopleService"/> substitutes.</summary>
internal sealed class ServiceCollectionStub(
    IGreetingService? greetingService = null,
    IPeopleService? peopleService = null) : IServiceProvider, IDisposable
{
    private readonly ServiceProvider _provider = new ServiceCollection()
        .AddLogging()
        .AddTransient(_ => greetingService ?? Substitute.For<IGreetingService>())
        .AddTransient(_ => peopleService ?? Substitute.For<IPeopleService>())
        .AddCommands()
        .BuildServiceProvider();

    public object? GetService(Type serviceType) => _provider.GetService(serviceType);

    public void Dispose() => _provider.Dispose();
}
