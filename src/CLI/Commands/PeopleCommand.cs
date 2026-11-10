using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace CLI.Commands;

internal sealed class PeopleCommand : Command
{
    private readonly Option<int> _limit = new("--limit", "-l")
    {
        Description = "Maximum number of people to list.",
        DefaultValueFactory = _ => 20
    };

    private const string _defaultCsvFileName = "people.csv";

    private readonly Option<FileInfo> _csv = new("--csv", "-c")
    {
        Description = $"Write the list to this CSV file (overwritten if it exists) instead of printing it. Without a file, writes {_defaultCsvFileName} next to the executable.",
        Arity = ArgumentArity.ZeroOrOne,
        CustomParser = result => new FileInfo(result.Tokens.Count == 0 ? Path.Combine(AppContext.BaseDirectory, _defaultCsvFileName) : result.Tokens[0].Value)
    };

    /// <param name="peopleServiceFactory">Resolved when the command runs, so <c>--help</c> works without a database configured.</param>
    /// <param name="logger">Logs the outcome of a CSV export.</param>
    public PeopleCommand(Func<IPeopleService> peopleServiceFactory, ILogger<PeopleCommand> logger)
        : base("people", "List people from the database.")
    {
        _limit.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int>() < 1)
            {
                result.AddError("--limit must be at least 1.");
            }
        });

        _csv.Validators.Add(result =>
        {
            var file = result.GetValueOrDefault<FileInfo>();
            if (file.Directory is { Exists: false } directory)
            {
                result.AddError($"--csv directory '{directory.FullName}' does not exist.");
            }
        });

        Options.Add(_limit);
        Options.Add(_csv);

        SetAction((parseResult, ct) => RunAsync(peopleServiceFactory, logger, parseResult, ct));
    }

    private async Task<int> RunAsync(Func<IPeopleService> peopleServiceFactory, ILogger<PeopleCommand> logger, ParseResult parseResult, CancellationToken ct)
    {
        var output = parseResult.InvocationConfiguration.Output;
        var error = parseResult.InvocationConfiguration.Error;

        try
        {
            var people = await peopleServiceFactory().ListAsync(parseResult.GetRequiredValue(_limit), ct);

            // GetResult: was --csv typed at all? `is { } csvFile`: non-null check that also assigns the FileInfo to csvFile.
            if (parseResult.GetResult(_csv) is not null && parseResult.GetValue(_csv) is { } csvFile)
            {
                await File.WriteAllTextAsync(csvFile.FullName, ToCsv(people), new UTF8Encoding(false), ct);
                logger.LogInformation("Wrote {Count} people to {Path}", people.Count, csvFile.FullName);
                return 0;
            }

            if (people.Count == 0)
            {
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"Could not write CSV file: {ex.Message}");
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

    private static string ToCsv(IReadOnlyList<PersonSummary> people)
    {
        // Header uses each PersonSummary property's [Description] when present, otherwise the property name.
        var header = string.Join(
            ',',
            typeof(PersonSummary).GetProperties().Select(p => EscapeCsv(p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? p.Name)));
        var builder = new StringBuilder(header).Append("\r\n");
        foreach (var person in people)
        {
            builder
                .Append(person.Id.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(EscapeCsv(person.Name)).Append(',')
                .Append(person.DateOfBirth.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)).Append(',')
                .Append(person.Salary.ToString("0.00", CultureInfo.InvariantCulture)).Append("\r\n");
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string value) =>
        value.AsSpan().IndexOfAny(",\"\r\n") >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}
