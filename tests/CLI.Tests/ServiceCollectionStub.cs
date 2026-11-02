using CLI.Services;

namespace CLI.Tests;

/// <summary>Minimal <see cref="IServiceProvider"/> that serves one <see cref="IGreetingService"/>.</summary>
internal sealed class ServiceCollectionStub(IGreetingService greetingService) : IServiceProvider
{
    public object? GetService(Type serviceType) =>
        serviceType == typeof(IGreetingService) ? greetingService : null;
}
