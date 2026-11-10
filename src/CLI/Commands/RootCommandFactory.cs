namespace CLI.Commands;

internal static class RootCommandFactory
{
    public static RootCommand Create(IServiceProvider services)
    {
        var root = new RootCommand("Command-line tool for the Service solution.");
        foreach (var command in services.GetServices<Command>())
        {
            root.Subcommands.Add(command);
        }

        return root;
    }
}
