using System.CommandLine;
using CLI.Services;

namespace CLI.Commands;

internal sealed class GreetCommand : Command
{
    private readonly Option<string> _name = new("--name", "-n")
    {
        Description = "Name of the person to greet.",
        Required = true
    };

    private readonly Option<DirectoryInfo> _outputDir = new("--output-dir", "-o")
    {
        Description = "Existing directory to write greeting.txt into. Omit to print only."
    };

    public GreetCommand(IGreetingService greetingService)
        : base("greet", "Greet someone by name.")
    {
        _name.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<string>();
            if (string.IsNullOrWhiteSpace(value))
            {
                result.AddError("--name must not be blank.");
            }
        });
        _outputDir.AcceptExistingOnly();

        Options.Add(_name);
        Options.Add(_outputDir);

        SetAction((parseResult, cancellationToken) => RunAsync(greetingService, parseResult, cancellationToken));
    }

    private async Task<int> RunAsync(IGreetingService greetingService, ParseResult parseResult, CancellationToken cancellationToken)
    {
        var output = parseResult.InvocationConfiguration.Output;
        var error = parseResult.InvocationConfiguration.Error;

        try
        {
            var greeting = await greetingService.GreetAsync(parseResult.GetRequiredValue(_name), cancellationToken);
            await output.WriteLineAsync(greeting);

            if (parseResult.GetValue(_outputDir) is { } directory)
            {
                var path = Path.Combine(directory.FullName, "greeting.txt");
                await File.WriteAllTextAsync(path, greeting + Environment.NewLine, cancellationToken);
                await output.WriteLineAsync($"Wrote {path}");
            }

            return 0;
        }
        catch (GreetingException ex)
        {
            await error.WriteLineAsync(ex.Message);
            return 1;
        }
        catch (OperationCanceledException)
        {
            await error.WriteLineAsync("Cancelled.");
            return 130;
        }
    }
}
