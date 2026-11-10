using Microsoft.Extensions.Logging;

namespace CLI.Tests;

/// <summary>Minimal <see cref="IServiceProvider"/> that serves the CLI services, defaulting to substitutes.</summary>
internal sealed class ServiceCollectionStub(
    IGreetingService? greetingService = null,
    IPeopleService? peopleService = null) : IServiceProvider
{
    private readonly IGreetingService _greetingService = greetingService ?? Substitute.For<IGreetingService>();
    private readonly IPeopleService _peopleService = peopleService ?? Substitute.For<IPeopleService>();

    public object? GetService(Type serviceType) =>
        serviceType == typeof(IGreetingService) ? _greetingService
        : serviceType == typeof(IPeopleService) ? _peopleService
        : serviceType == typeof(ILogger<PeopleCommand>) ? NullLogger<PeopleCommand>.Instance
        : null;
}
