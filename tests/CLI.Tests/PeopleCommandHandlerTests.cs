using System.CommandLine;
using CLI.Commands;
using CLI.Services;
using NSubstitute;

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
    public async Task People_WhenEmpty_PrintsNoPeopleFound()
    {
        _ = _peopleService.ListAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var exitCode = await RunAsync("people");

        Assert.Equal(0, exitCode);
        Assert.Contains("No people found.", _output.ToString());
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
}
