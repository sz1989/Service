using System.CommandLine;
using CLI.Commands;
using CLI.Services;
using FluentAssertions;
using NSubstitute;

namespace CLI.Tests;

public class GreetCommandHandlerTests
{
    private readonly IGreetingService _greetingService = Substitute.For<IGreetingService>();
    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    private Task<int> RunAsync(params string[] args)
    {
        var root = RootCommandFactory.Create(new ServiceCollectionStub(_greetingService));
        return root.Parse(args).InvokeAsync(new InvocationConfiguration { Output = _output, Error = _error });
    }

    [Fact]
    public async Task Greet_PrintsGreeting_AndReturnsZero()
    {
        _greetingService.GreetAsync("Ada", Arg.Any<CancellationToken>()).Returns("Hello, Ada!");

        var exitCode = await RunAsync("greet", "--name", "Ada");

        exitCode.Should().Be(0);
        _output.ToString().Should().Contain("Hello, Ada!");
        _error.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task Greet_WhenServiceThrows_ReturnsNonZero_WithFriendlyMessage()
    {
        _greetingService.GreetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new GreetingException("boom"));

        var exitCode = await RunAsync("greet", "--name", "Ada");

        exitCode.Should().NotBe(0);
        _error.ToString().Should().Contain("boom").And.NotContain("   at ");
    }

    [Fact]
    public async Task Greet_WithOutputDir_WritesGreetingFile()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            _greetingService.GreetAsync("Ada", Arg.Any<CancellationToken>()).Returns("Hello, Ada!");

            var exitCode = await RunAsync("greet", "--name", "Ada", "--output-dir", directory.FullName);

            exitCode.Should().Be(0);
            File.ReadAllText(Path.Combine(directory.FullName, "greeting.txt")).Should().Contain("Hello, Ada!");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}

public class GreetingServiceTests
{
    [Fact]
    public async Task GreetAsync_TrimsName()
    {
        var greeting = await new GreetingService().GreetAsync("  Ada ", CancellationToken.None);

        greeting.Should().Be("Hello, Ada!");
    }

    [Fact]
    public async Task GreetAsync_WithBlankName_Throws()
    {
        var act = () => new GreetingService().GreetAsync(" ", CancellationToken.None);

        await act.Should().ThrowAsync<GreetingException>();
    }
}
