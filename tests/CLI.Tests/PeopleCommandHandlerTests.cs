namespace CLI.Tests;

public sealed class PeopleCommandHandlerTests : IDisposable
{
    private readonly IPeopleService _peopleService = Substitute.For<IPeopleService>();
    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    public void Dispose()
    {
        _output.Dispose();
        _error.Dispose();
    }

    private Task<int> RunAsync(params string[] args)
    {
        var root = RootCommandFactory.Create(new ServiceCollectionStub(peopleService: _peopleService));
        return root.Parse(args).InvokeAsync(
            new InvocationConfiguration { Output = _output, Error = _error },
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task People_PrintsEachPerson_AndReturnsZero()
    {
        PersonSummary[] people = [new(1, "Ada", new DateOnly(1990, 1, 2), 1234.5m)];
        _ = _peopleService.ListAsync(20, Arg.Any<CancellationToken>()).Returns(people);

        var exitCode = await RunAsync("people");

        Assert.Equal(0, exitCode);
        Assert.Contains("1\tAda\t1990-01-02\t1234.50", _output.ToString());
        Assert.Empty(_error.ToString());
    }

    [Fact]
    public async Task People_PassesLimitToService()
    {
        _ = _peopleService.ListAsync(5, Arg.Any<CancellationToken>()).Returns([]);

        var exitCode = await RunAsync("people", "--limit", "5");

        Assert.Equal(0, exitCode);
        _ = await _peopleService.Received(1).ListAsync(5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task People_WhenServiceThrows_ReturnsNonZero_WithFriendlyMessage()
    {
        _ = _peopleService.ListAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<PersonSummary>>(_ => throw new PeopleServiceException("boom"));

        var exitCode = await RunAsync("people");

        Assert.NotEqual(0, exitCode);
        Assert.Contains("boom", _error.ToString());
        Assert.DoesNotContain("   at ", _error.ToString());
    }

    [Fact]
    public async Task People_WhenCancelled_Returns130()
    {
        _ = _peopleService.ListAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<PersonSummary>>(_ => throw new OperationCanceledException());

        var exitCode = await RunAsync("people");

        Assert.Equal(130, exitCode);
        Assert.Contains("Cancelled.", _error.ToString());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void People_WithNonPositiveLimit_ReportsError(string limit)
    {
        var root = RootCommandFactory.Create(new ServiceCollectionStub(peopleService: _peopleService));

        var result = root.Parse(["people", "--limit", limit]);

        var error = Assert.Single(result.Errors);
        Assert.Contains("--limit must be at least 1", error.Message);
    }

    [Fact]
    public async Task People_WithCsvFormat_WritesFile_AndDoesNotPrintRows()
    {
        PersonSummary[] people =
        [
            new(1, "Ada", new DateOnly(1990, 1, 2), 1234.5m),
            new(2, "Lovelace, \"Ada\"", new DateOnly(1985, 12, 31), 10m)
        ];
        _ = _peopleService.ListAsync(20, Arg.Any<CancellationToken>()).Returns(people);
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");

        try
        {
            var exitCode = await RunAsync("people", "--output-format", "csv", "--output-file-name", path);

            Assert.Equal(0, exitCode);
            Assert.Equal(
                "Id,Name,Date of birth,Annual salary\r\n1,Ada,01/02/1990,1234.50\r\n2,\"Lovelace, \"\"Ada\"\"\",12/31/1985,10.00\r\n",
                await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
            Assert.Empty(_output.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task People_WithMarkdownFormat_WritesTable_AndDoesNotPrintRows()
    {
        PersonSummary[] people =
        [
            new(1, "Ada", new DateOnly(1990, 1, 2), 1234.5m),
            new(2, "Love|lace", new DateOnly(1985, 12, 31), 10m)
        ];
        _ = _peopleService.ListAsync(20, Arg.Any<CancellationToken>()).Returns(people);
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.md");

        try
        {
            var exitCode = await RunAsync("people", "--output-format", "markdown", "--output-file-name", path);

            Assert.Equal(0, exitCode);
            Assert.Equal(
                "| Id | Name | Date of birth | Annual salary |\n| ---: | --- | --- | ---: |\n| 1 | Ada | 1990-01-02 | 1234.50 |\n| 2 | Love\\|lace | 1985-12-31 | 10.00 |\n",
                await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
            Assert.Empty(_output.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task People_WithPdfFormat_WritesPdfFile_AndDoesNotPrintRows()
    {
        PersonSummary[] people = [new(1, "Ada", new DateOnly(1990, 1, 2), 1234.5m)];
        _ = _peopleService.ListAsync(20, Arg.Any<CancellationToken>()).Returns(people);
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");

        try
        {
            var exitCode = await RunAsync("people", "--output-format", "pdf", "--output-file-name", path);

            Assert.Equal(0, exitCode);
            var bytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
            Assert.Empty(_output.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    // One test for all formats: the default files share the executable folder, so they must not run in parallel.
    [Theory]
    [InlineData("csv", "People.csv")]
    [InlineData("markdown", "People.md")]
    [InlineData("pdf", "People.pdf")]
    public async Task People_WithFormatAndNoFileName_WritesDefaultFileInExecutableFolder(string format, string expectedFileName)
    {
        PersonSummary[] people = [new(1, "Ada", new DateOnly(1990, 1, 2), 1234.5m)];
        _ = _peopleService.ListAsync(20, Arg.Any<CancellationToken>()).Returns(people);
        var path = Path.Combine(AppContext.BaseDirectory, expectedFileName);

        try
        {
            var exitCode = await RunAsync("people", "--output-format", format);

            Assert.Equal(0, exitCode);
            Assert.Contains(expectedFileName, Directory.EnumerateFiles(AppContext.BaseDirectory).Select(Path.GetFileName));
            Assert.Empty(_output.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task People_WithCamelCaseAliases_WritesFile()
    {
        _ = _peopleService.ListAsync(20, Arg.Any<CancellationToken>()).Returns([]);
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");

        try
        {
            var exitCode = await RunAsync("people", "--outputFormat", "csv", "--outputFileName", path);

            Assert.Equal(0, exitCode);
            Assert.Equal(
                "Id,Name,Date of birth,Annual salary\r\n",
                await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void People_WithOutputFileNameInMissingDirectory_ReportsError()
    {
        var root = RootCommandFactory.Create(new ServiceCollectionStub(peopleService: _peopleService));
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "people.csv");

        var result = root.Parse(["people", "--output-format", "csv", "--output-file-name", path]);

        var error = Assert.Single(result.Errors);
        Assert.Contains("--output-file-name directory", error.Message);
    }

    [Fact]
    public void People_WithOutputFileNameButNoFormat_ReportsError()
    {
        var root = RootCommandFactory.Create(new ServiceCollectionStub(peopleService: _peopleService));

        var result = root.Parse(["people", "--output-file-name", Path.Combine(Path.GetTempPath(), "people.csv")]);

        var error = Assert.Single(result.Errors);
        Assert.Contains("--output-file-name requires --output-format", error.Message);
    }

    [Fact]
    public void People_WithUnknownFormat_ReportsError()
    {
        var root = RootCommandFactory.Create(new ServiceCollectionStub(peopleService: _peopleService));

        var result = root.Parse(["people", "--output-format", "xml"]);

        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData("--csv")]
    [InlineData("--markdown")]
    [InlineData("--pdf")]
    public void People_WithRemovedExportOption_ReportsError(string option)
    {
        var root = RootCommandFactory.Create(new ServiceCollectionStub(peopleService: _peopleService));

        var result = root.Parse(["people", option]);

        Assert.NotEmpty(result.Errors);
    }
}
