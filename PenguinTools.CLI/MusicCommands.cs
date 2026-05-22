using System.CommandLine;
using PenguinTools.i18n;

namespace PenguinTools.CLI;

internal static class MusicCommands
{
    internal static Command BuildMusicCommand()
    {
        var command = new Command("music", Strings.Cli_Cmd_music);
        command.Subcommands.Add(BuildMusicExportCommand());
        return command;
    }

    private static Command BuildMusicExportCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = Strings.Cli_Arg_chart_input
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = Strings.Cli_Arg_music_output
        };
        var jacketInputOption = new Option<string?>("--jacket-input")
        {
            Description = Strings.Cli_Arg_jacket_override
        };
        var audioOptions = CommandLineOptions.CreateAudioCommandOptions();
        var stageOptions = CommandLineOptions.CreateStageCommandOptions();

        var command = new Command("export", Strings.Cli_Cmd_music_export);
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        command.Options.Add(jacketInputOption);
        CommandLineOptions.AddAudioCommandOptions(command, audioOptions);
        CommandLineOptions.AddStageCommandOptions(command, stageOptions);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var jacketInput = CliPaths.ResolveOptionalPath(parseResult.GetValue(jacketInputOption));
            var audioOverrides = CommandLineOptions.GetAudioRequestOverrides(parseResult, audioOptions);
            var stageOverrides = CommandLineOptions.GetStageRequestOverrides(parseResult, stageOptions);
            var outputOptions = RootCommands.GetOutputOptions(parseResult);

            return await CliOperations.ExecuteAsync("music export", outputOptions, async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                    return new CliCommandOutcome(parsed.ToResult(),
                        Data: new CliCommandData(input, OutputDirectory: output));

                var exported = await CliOperations.ExportMusicAsync(runtime, parsed.Value, output, jacketInput,
                    audioOverrides, stageOverrides, ct);
                var result = CliPaths.Merge(parsed.Diagnostics, exported);
                var data = CliOperations.CreateMusicData(input, output, parsed.Value.Meta, jacketInput, stageOverrides);
                var message = result.Succeeded ? string.Format(Strings.Cli_Msg_exported_music, output) : null;
                return new CliCommandOutcome(result, message, data);
            }, cancellationToken);
        });

        return command;
    }
}
