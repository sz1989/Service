using System.CommandLine;
using CLI.Commands;
using CLI.Services;
using FluentAssertions;
using NSubstitute;

namespace CLI.Tests;

public class GreetCommandParserTests
{
    private static RootCommand CreateRoot()
    {
        var services = new ServiceCollectionStub(Substitute.For<IGreetingService>());
        return RootCommandFactory.Create(services);
    }

    [Fact]
    public void Parse_WithValidName_HasNoErrors()
    {
        var result = CreateRoot().Parse(["greet", "--name", "Ada"]);

        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithoutName_ReportsError()
    {
        var result = CreateRoot().Parse(["greet"]);

        result.Errors.Should().ContainSingle().Which.Message.Should().Contain("--name");
    }

    [Fact]
    public void Parse_WithBlankName_ReportsError()
    {
        var result = CreateRoot().Parse(["greet", "--name", " "]);

        result.Errors.Should().ContainSingle().Which.Message.Should().Contain("must not be blank");
    }

    [Fact]
    public void Parse_WithUnknownOption_ReportsError()
    {
        var result = CreateRoot().Parse(["greet", "--name", "Ada", "--bogus"]);

        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Parse_WithMissingOutputDir_ReportsError()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var result = CreateRoot().Parse(["greet", "--name", "Ada", "--output-dir", missing]);

        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Parse_WithExistingOutputDir_HasNoErrors()
    {
        var result = CreateRoot().Parse(["greet", "--name", "Ada", "--output-dir", Path.GetTempPath()]);

        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Help_ListsOptions()
    {
        var output = new StringWriter();

        var exitCode = await CreateRoot().Parse(["greet", "--help"])
            .InvokeAsync(new InvocationConfiguration { Output = output }, TestContext.Current.CancellationToken);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("--name").And.Contain("--output-dir");
    }
}
