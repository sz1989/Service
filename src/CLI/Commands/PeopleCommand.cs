using System.CommandLine;
using CLI.Services;

namespace CLI.Commands;

internal sealed class PeopleCommand : Command
{
    private readonly Option<int> _limit = new("--limit", "-l")
    {
        Description = "Maximum number of people to list.",
        DefaultValueFactory = _ => 20
    };

    /// <param name="peopleServiceFactory">Resolved when the command runs, so <c>--help</c> works without a database configured.</param>
    public PeopleCommand(Func<IPeopleService> peopleServiceFactory)
        : base("people", "List people from the database.")
    {
        _limit.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int>() < 1)
            {
                result.AddError("--limit must be at least 1.");
            }
        });

        Options.Add(_limit);

        SetAction((parseResult, ct) => RunAsync(peopleServiceFactory, parseResult, ct));
    }

    private async Task<int> RunAsync(Func<IPeopleService> peopleServiceFactory, ParseResult parseResult, CancellationToken ct)
    {
        var output = parseResult.InvocationConfiguration.Output;
        var error = parseResult.InvocationConfiguration.Error;

        try
        {
            var people = await peopleServiceFactory().ListAsync(parseResult.GetRequiredValue(_limit), ct);

            if (people.Count == 0)
            {
                await output.WriteLineAsync("No people found.");
                return 0;
            }

            foreach (var person in people)
            {
                await output.WriteLineAsync($"{person.Id}\t{person.Name}\t{person.DateOfBirth:yyyy-MM-dd}\t{person.Salary:0.00}");
            }

            return 0;
        }
        catch (PeopleServiceException ex)
        {
            await error.WriteLineAsync(ex.Message);
            return 1;
        }
        catch (InvalidOperationException ex)
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
