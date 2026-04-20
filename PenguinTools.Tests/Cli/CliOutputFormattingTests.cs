using PenguinTools.CLI;
using PenguinTools.Core;
using Xunit;

namespace PenguinTools.Tests.Cli;

public class CliOutputFormattingTests
{
    [Fact]
    public void OutputOptions_DefaultToPrettyJson()
    {
        var parseResult = RootCommands.BuildRootCommand().Parse(["scan", "."]);

        var options = RootCommands.GetOutputOptions(parseResult);

        Assert.Equal(CliOutputFormat.Json, options.Format);
        Assert.True(options.PrettyJson);
    }

    [Fact]
    public void OutputOptions_DisablePrettyJson_WhenNoPrettyIsSpecified()
    {
        var parseResult = RootCommands.BuildRootCommand().Parse(["--no-pretty", "scan", "."]);

        var options = RootCommands.GetOutputOptions(parseResult);

        Assert.Equal(CliOutputFormat.Json, options.Format);
        Assert.False(options.PrettyJson);
    }

    [Fact]
    public void SerializeJson_IndentsByDefault_AndCanEmitCompactJson()
    {
        var outcome = new CliCommandOutcome(
            OperationResult.Success(),
            "Done.",
            new CliCommandData("input"));

        var prettyJson = CliOutput.SerializeJson("scan", 0, outcome, true);
        var compactJson = CliOutput.SerializeJson("scan", 0, outcome, false);

        Assert.Contains(Environment.NewLine, prettyJson);
        Assert.Contains("  \"schemaVersion\": 1,", prettyJson);
        Assert.DoesNotContain(Environment.NewLine, compactJson);
    }
}