using System.CommandLine;
using PenguinTools.Core;

namespace PenguinTools.CLI;

internal static class WorkflowCommands
{
    internal static Command BuildWorkflowCommand()
    {
        var command = new Command("workflow", "Export mgxc with jacket and audio.");
        command.Subcommands.Add(BuildWorkflowExportCommand());
        return command;
    }

    private static Command BuildWorkflowExportCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = "Path to the source .mgxc chart."
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = "Base folder for the exported workflow files."
        };
        var assetRootOption = new Option<string?>("--asset-root")
        {
            Description = "Optional directory to scan for additional asset XML before parsing."
        };
        var jacketInputOption = new Option<string?>("--jacket-input")
        {
            Description = "Override the jacket source path used for export."
        };
        var audioOptions = CommandLineOptions.CreateAudioCommandOptions();
        var stageOptions = CommandLineOptions.CreateStageCommandOptions();

        var command = new Command("export", "Export chart, jacket, audio, and optional stage/event XML from one MGXC chart.");
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        command.Options.Add(assetRootOption);
        command.Options.Add(jacketInputOption);
        CommandLineOptions.AddAudioCommandOptions(command, audioOptions);
        CommandLineOptions.AddStageCommandOptions(command, stageOptions);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var assetRoot = CliPaths.ResolveOptionalPath(parseResult.GetValue(assetRootOption));
            var jacketInput = CliPaths.ResolveOptionalPath(parseResult.GetValue(jacketInputOption));
            var audioOverrides = CommandLineOptions.GetAudioRequestOverrides(parseResult, audioOptions);
            var stageOverrides = CommandLineOptions.GetStageRequestOverrides(parseResult, stageOptions);
            var outputFormat = RootCommands.GetOutputFormat(parseResult);

            return await CliOperations.ExecuteAsync("workflow export", outputFormat, async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, assetRoot, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                {
                    return new CliCommandOutcome(parsed.ToResult(), Data: new CliCommandData(InputPath: input, OutputDirectory: output, AssetRoot: assetRoot));
                }

                var exported = await CliOperations.ExportWorkflowAsync(runtime, parsed.Value, output, jacketInput, audioOverrides, stageOverrides, ct);
                var result = CliPaths.Merge(parsed.Diagnostics, exported);
                var data = CliOperations.CreateWorkflowData(input, output, assetRoot, parsed.Value.Meta, jacketInput, stageOverrides);
                var message = result.Succeeded ? $"Exported workflow: {output}" : null;
                return new CliCommandOutcome(result, message, data);
            }, cancellationToken);
        });

        return command;
    }
}
