using System.CommandLine;
using PenguinTools.Core;

namespace PenguinTools.CLI;

internal static class MusicCommands
{
    internal static Command BuildMusicCommand()
    {
        var command = new Command("music", "Export an MGXC or UGC chart with jacket and audio.");
        command.Subcommands.Add(BuildMusicExportCommand());
        return command;
    }

    private static Command BuildMusicExportCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = "Path to the source chart (.mgxc or .ugc)."
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = "Base folder for the exported music bundle files."
        };
        var jacketInputOption = new Option<string?>("--jacket-input")
        {
            Description = "Override the jacket source path used for export."
        };
        var audioOptions = CommandLineOptions.CreateAudioCommandOptions();
        var stageOptions = CommandLineOptions.CreateStageCommandOptions();

        var command = new Command("export", "Export chart, jacket, audio, and optional stage/event XML from one MGXC or UGC chart.");
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
            var outputFormat = RootCommands.GetOutputFormat(parseResult);

            return await CliOperations.ExecuteAsync("music export", outputFormat, async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                {
                    return new CliCommandOutcome(parsed.ToResult(), Data: new CliCommandData(InputPath: input, OutputDirectory: output));
                }

                var exported = await CliOperations.ExportMusicAsync(runtime, parsed.Value, output, jacketInput, audioOverrides, stageOverrides, ct);
                var result = CliPaths.Merge(parsed.Diagnostics, exported);
                var data = CliOperations.CreateMusicData(input, output, parsed.Value.Meta, jacketInput, stageOverrides);
                var message = result.Succeeded ? $"Exported music: {output}" : null;
                return new CliCommandOutcome(result, message, data);
            }, cancellationToken);
        });

        return command;
    }
}
