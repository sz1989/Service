using System.CommandLine;
using CLI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CLI.Commands;

internal static class RootCommandFactory
{
    public static RootCommand Create(IServiceProvider services)
    {
        var root = new RootCommand("Command-line tool for the Service solution.");
        root.Subcommands.Add(new GreetCommand(services.GetRequiredService<IGreetingService>()));
        root.Subcommands.Add(new PeopleCommand(services.GetRequiredService<IPeopleService>));
        return root;
    }
}
