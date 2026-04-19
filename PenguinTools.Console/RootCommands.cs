using System.CommandLine;

namespace PenguinTools.CLI;

internal static class RootCommands
{
    private static readonly Option<CliOutputFormat> OutputFormatOption = new("--format", "--output-format")
    {
        Description = "Set the CLI output format. Defaults to json."
    };

    internal static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("Command-line tools for chart conversion and asset export.");
        rootCommand.Options.Add(OutputFormatOption);
        rootCommand.Subcommands.Add(ChartCommands.BuildChartCommand());
        rootCommand.Subcommands.Add(WorkflowCommands.BuildWorkflowCommand());
        rootCommand.Subcommands.Add(MediaCommands.BuildMediaCommand());
        return rootCommand;
    }

    internal static CliOutputFormat GetOutputFormat(ParseResult parseResult)
    {
        return parseResult.GetValue(OutputFormatOption);
    }
}
