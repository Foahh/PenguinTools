using System.CommandLine;

namespace PenguinTools.CLI;

internal static class RootCommands
{
    private static readonly Option<CliOutputFormat> OutputFormatOption = new("--format", "--output-format")
    {
        Description = "Set the CLI output format. Defaults to json."
    };

    private static readonly Option<bool> NoPrettyOption = new("--no-pretty")
    {
        Description = "Emit compact JSON instead of pretty-printed JSON."
    };

    internal static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("Command-line tools for chart conversion and asset export.");
        rootCommand.Options.Add(OutputFormatOption);
        rootCommand.Options.Add(NoPrettyOption);
        rootCommand.Subcommands.Add(ScanCommands.BuildScanCommand());
        rootCommand.Subcommands.Add(ChartCommands.BuildChartCommand());
        rootCommand.Subcommands.Add(MusicCommands.BuildMusicCommand());
        rootCommand.Subcommands.Add(OptionCommands.BuildOptionCommand());
        rootCommand.Subcommands.Add(MediaCommands.BuildMediaCommand());
        rootCommand.Subcommands.Add(AssetCommands.BuildAssetCommand());
        return rootCommand;
    }

    internal static CliOutputOptions GetOutputOptions(ParseResult parseResult)
    {
        return new CliOutputOptions(
            parseResult.GetValue(OutputFormatOption),
            !parseResult.GetValue(NoPrettyOption));
    }
}
