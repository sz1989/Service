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

    private const string _defaultFileNameWithoutExtension = "People";

    private readonly Option<OutputFormat?> _outputFormat = new("--output-format", "--outputFormat", "-f")
    {
        Description = "Write the list to a file in this format (csv, markdown or pdf) instead of printing it."
    };

    private readonly Option<FileInfo> _outputFileName = new("--output-file-name", "--outputFileName", "-o")
    {
        Description = $"File (optionally with a path) to write when --output-format is set; overwritten if it exists. Defaults to {_defaultFileNameWithoutExtension}.<ext> for the format (e.g. {_defaultFileNameWithoutExtension}.csv) next to the executable."
    };

    /// <param name="peopleServiceFactory">Resolved when the command runs, so <c>--help</c> works without a database configured.</param>
    /// <param name="logger">Logs the outcome of a CSV, Markdown or PDF export.</param>
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

        _outputFileName.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<FileInfo>() is { Directory: { Exists: false } directory })
            {
                result.AddError($"--output-file-name directory '{directory.FullName}' does not exist.");
            }
        });

        Validators.Add(result =>
        {
            if (result.GetResult(_outputFileName) is not null && result.GetResult(_outputFormat) is null)
            {
                result.AddError("--output-file-name requires --output-format.");
            }
        });

        Options.Add(_limit);
        Options.Add(_outputFormat);
        Options.Add(_outputFileName);

        SetAction((parseResult, ct) => RunAsync(peopleServiceFactory, logger, parseResult, ct));
    }

    private async Task<int> RunAsync(Func<IPeopleService> peopleServiceFactory, ILogger<PeopleCommand> logger, ParseResult parseResult, CancellationToken ct)
    {
        var output = parseResult.InvocationConfiguration.Output;
        var error = parseResult.InvocationConfiguration.Error;

        try
        {
            var people = await peopleServiceFactory().ListAsync(parseResult.GetRequiredValue(_limit), ct);

            if (parseResult.GetValue(_outputFormat) is { } format)
            {
                var file = parseResult.GetValue(_outputFileName)
                    ?? new FileInfo(Path.Combine(AppContext.BaseDirectory, _defaultFileNameWithoutExtension + DefaultExtension(format)));

                switch (format)
                {
                    case OutputFormat.Csv:
                        await File.WriteAllTextAsync(file.FullName, ToCsv(people), new UTF8Encoding(false), ct);
                        break;
                    case OutputFormat.Markdown:
                        await File.WriteAllTextAsync(file.FullName, ToMarkdown(people), new UTF8Encoding(false), ct);
                        break;
                    case OutputFormat.Pdf:
                        await File.WriteAllBytesAsync(file.FullName, PeoplePdf.Render(people), ct);
                        break;
                    default:
                        await error.WriteLineAsync($"Unsupported output format '{format}'.");
                        return 1;
                }

                logger.LogInformation("Wrote {Count} people to {Path}", people.Count, file.FullName);
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
            await error.WriteLineAsync($"Could not write file: {ex.Message}");
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

    private static string DefaultExtension(OutputFormat format) => format switch
    {
        OutputFormat.Csv => ".csv",
        OutputFormat.Markdown => ".md",
        OutputFormat.Pdf => ".pdf",
        _ => throw new InvalidOperationException($"Unsupported output format '{format}'.")
    };

    // Header uses each PersonSummary property's [Description] when present, otherwise the property name.
    private static IEnumerable<string> HeaderNames() =>
        typeof(PersonSummary).GetProperties().Select(p => p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? p.Name);

    private static string ToCsv(IReadOnlyList<PersonSummary> people)
    {
        var header = string.Join(',', HeaderNames().Select(EscapeCsv));
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

    private static string ToMarkdown(IReadOnlyList<PersonSummary> people)
    {
        // Salary column is right-aligned (`---:`); the rest default to left.
        var builder = new StringBuilder()
            .Append("| ").AppendJoin(" | ", HeaderNames()).Append(" |\n")
            .Append("| ---: | --- | --- | ---: |\n");
        foreach (var person in people)
        {
            builder
                .Append("| ").Append(person.Id.ToString(CultureInfo.InvariantCulture))
                .Append(" | ").Append(EscapeMarkdown(person.Name))
                .Append(" | ").Append(person.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                .Append(" | ").Append(person.Salary.ToString("0.00", CultureInfo.InvariantCulture))
                .Append(" |\n");
        }

        return builder.ToString();
    }

    private static string EscapeMarkdown(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal);

    private static string EscapeCsv(string value) =>
        value.AsSpan().IndexOfAny(",\"\r\n") >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}
