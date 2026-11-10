namespace CLI.Commands;

internal static class CommandServiceCollectionExtensions
{
    /// <summary>Registers every CLI command so <see cref="RootCommandFactory"/> can resolve them from the container.</summary>
    public static IServiceCollection AddCommands(this IServiceCollection services)
    {
        // Lets PeopleCommand resolve the service when the command runs, so --help works without a database configured.
        services.AddTransient<Func<IPeopleService>>(sp => sp.GetRequiredService<IPeopleService>);
        services.AddTransient<Command, GreetCommand>();
        services.AddTransient<Command, PeopleCommand>();
        return services;
    }
}
