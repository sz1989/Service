namespace CLI.Tests;

public sealed class GreetCommandHandlerTests : IDisposable
{
    private readonly IGreetingService _greetingService = Substitute.For<IGreetingService>();
    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    public void Dispose()
    {
        _output.Dispose();
        _error.Dispose();
    }

    private Task<int> RunAsync(params string[] args)
    {
        var root = RootCommandFactory.Create(new ServiceCollectionStub(_greetingService));
        return root.Parse(args).InvokeAsync(
            new InvocationConfiguration { Output = _output, Error = _error },
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Greet_PrintsGreeting_AndReturnsZero()
    {
        _ = _greetingService.GreetAsync("Ada", Arg.Any<CancellationToken>()).Returns("Hello, Ada!");

        var exitCode = await RunAsync("greet", "--name", "Ada");

        Assert.Equal(0, exitCode);
        Assert.Contains("Hello, Ada!", _output.ToString());
        Assert.Empty(_error.ToString());
    }

    [Fact]
    public async Task Greet_WhenServiceThrows_ReturnsNonZero_WithFriendlyMessage()
    {
        _ = _greetingService.GreetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new GreetingException("boom"));

        var exitCode = await RunAsync("greet", "--name", "Ada");

        Assert.NotEqual(0, exitCode);
        Assert.Contains("boom", _error.ToString());
        Assert.DoesNotContain("   at ", _error.ToString());
    }

    [Fact]
    public async Task Greet_WhenCancelled_Returns130()
    {
        _greetingService.GreetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new OperationCanceledException());

        var exitCode = await RunAsync("greet", "--name", "Ada");

        Assert.Equal(130, exitCode);
        Assert.Contains("Cancelled.", _error.ToString());
    }

    [Fact]
    public async Task Greet_WithOutputDir_WritesGreetingFile()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            _ = _greetingService.GreetAsync("Ada", Arg.Any<CancellationToken>()).Returns("Hello, Ada!");

            var exitCode = await RunAsync("greet", "--name", "Ada", "--output-dir", directory.FullName);

            Assert.Equal(0, exitCode);
            var written = await File.ReadAllTextAsync(
                Path.Combine(directory.FullName, "greeting.txt"), TestContext.Current.CancellationToken);
            Assert.Contains("Hello, Ada!", written);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}

public class GreetingServiceTests
{
    private static GreetingService CreateService() => new(NullLogger<GreetingService>.Instance);

    [Theory]
    [InlineData("  Ada ", "Hello, Ada!")]
    [InlineData("Ada", "Hello, Ada!")]
    public async Task GreetAsync_TrimsName(string name, string expected)
    {
        var greeting = await CreateService().GreetAsync(name, TestContext.Current.CancellationToken);

        Assert.Equal(expected, greeting);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GreetAsync_WithBlankName_Throws(string name)
    {
        await Assert.ThrowsAsync<GreetingException>(
            () => CreateService().GreetAsync(name, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GreetAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateService().GreetAsync("Ada", cts.Token));
    }
}
