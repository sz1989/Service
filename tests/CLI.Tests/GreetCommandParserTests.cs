using System.CommandLine;
using CLI.Commands;
using CLI.Services;
using NSubstitute;

namespace CLI.Tests;

public class GreetCommandParserTests
{
    private static RootCommand CreateRoot() =>
        RootCommandFactory.Create(new ServiceCollectionStub(Substitute.For<IGreetingService>()));

    [Fact]
    public void Parse_WithValidName_HasNoErrors()
    {
        var result = CreateRoot().Parse(["greet", "--name", "Ada"]);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithoutName_ReportsError()
    {
        var result = CreateRoot().Parse(["greet"]);

        var error = Assert.Single(result.Errors);
        Assert.Contains("--name", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Parse_WithBlankName_ReportsError(string name)
    {
        var result = CreateRoot().Parse(["greet", "--name", name]);

        var error = Assert.Single(result.Errors);
        Assert.Contains("must not be blank", error.Message);
    }

    [Fact]
    public void Parse_WithUnknownOption_ReportsError()
    {
        var result = CreateRoot().Parse(["greet", "--name", "Ada", "--bogus"]);

        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_WithMissingOutputDir_ReportsError()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var result = CreateRoot().Parse(["greet", "--name", "Ada", "--output-dir", missing]);

        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_WithExistingOutputDir_HasNoErrors()
    {
        var result = CreateRoot().Parse(["greet", "--name", "Ada", "--output-dir", Path.GetTempPath()]);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Help_ListsOptions()
    {
        var output = new StringWriter();

        var exitCode = await CreateRoot().Parse(["greet", "--help"])
            .InvokeAsync(new InvocationConfiguration { Output = output }, TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Contains("--name", output.ToString());
        Assert.Contains("--output-dir", output.ToString());
    }
}
