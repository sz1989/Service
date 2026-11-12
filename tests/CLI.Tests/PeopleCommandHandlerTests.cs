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
    public async Task People_WithCsv_WritesFile_AndDoesNotPrintRows()
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
            var exitCode = await RunAsync("people", "--csv", path);

            Assert.Equal(0, exitCode);
            Assert.Equal(
                "Id,Name,Date of birth,Annual salary\r\n1,Ada,01/02/1990,1234.50\r\n2,\"Lovelace, \"\"Ada\"\"\",12/31/1985,10.00\r\n",
                await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
            Assert.Empty(_output.ToString());
            Assert.DoesNotContain("Ada\t", _output.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void People_WithCsvInMissingDirectory_ReportsError()
    {
        var root = RootCommandFactory.Create(new ServiceCollectionStub(peopleService: _peopleService));
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "people.csv");

        var result = root.Parse(["people", "--csv", path]);

        var error = Assert.Single(result.Errors);
        Assert.Contains("does not exist", error.Message);
    }

    [Fact]
    public async Task People_WithCsvAndNoFile_WritesDefaultFileInExecutableFolder()
    {
        PersonSummary[] people = [new(1, "Ada", new DateOnly(1990, 1, 2), 1234.5m)];
        _ = _peopleService.ListAsync(20, Arg.Any<CancellationToken>()).Returns(people);
        var path = Path.Combine(AppContext.BaseDirectory, "people.csv");

        try
        {
            var exitCode = await RunAsync("people", "--csv");

            Assert.Equal(0, exitCode);
            Assert.Equal(
                "Id,Name,Date of birth,Annual salary\r\n1,Ada,01/02/1990,1234.50\r\n",
                await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task People_WithMarkdown_WritesTable_AndDoesNotPrintRows()
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
            var exitCode = await RunAsync("people", "--markdown", path);

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
    public void People_WithMarkdownInMissingDirectory_ReportsError()
    {
        var root = RootCommandFactory.Create(new ServiceCollectionStub(peopleService: _peopleService));
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "people.md");

        var result = root.Parse(["people", "--markdown", path]);

        var error = Assert.Single(result.Errors);
        Assert.Contains("--markdown directory", error.Message);
    }
}
