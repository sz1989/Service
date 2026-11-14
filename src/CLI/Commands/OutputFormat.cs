namespace CLI.Commands;

/// <summary>File formats the <c>people</c> command can export to via <c>--output-format</c>.</summary>
internal enum OutputFormat
{
    Csv,
    Markdown,
    Pdf
}
