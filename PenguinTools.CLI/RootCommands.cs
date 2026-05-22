using System.CommandLine;
using PenguinTools.i18n;

namespace PenguinTools.CLI;

internal static class RootCommands
{
    private static readonly Option<CliOutputFormat> OutputFormatOption = new("--format", "--output-format")
    {
        Description = Strings.Cli_Opt_output_format
    };

    private static readonly Option<bool> NoPrettyOption = new("--no-pretty")
    {
        Description = Strings.Cli_Opt_compact_json
    };

    internal static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand(Strings.Cli_Root_description);
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
