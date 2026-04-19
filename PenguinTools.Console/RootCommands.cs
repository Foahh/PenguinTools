using System.CommandLine;

namespace PenguinTools.CLI;

internal static class RootCommands
{
    internal static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("Command-line tools for chart conversion and asset export.");
        rootCommand.Subcommands.Add(ChartCommands.BuildChartCommand());
        rootCommand.Subcommands.Add(WorkflowCommands.BuildWorkflowCommand());
        rootCommand.Subcommands.Add(MediaCommands.BuildMediaCommand());
        return rootCommand;
    }
}
