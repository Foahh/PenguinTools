using System.CommandLine;
using PenguinTools.Core;
using PenguinTools.Media;

namespace PenguinTools.CLI;

internal static class MediaCommands
{
    internal static Command BuildMediaCommand()
    {
        var command = new Command("media", "Media conversion utilities.");
        command.Subcommands.Add(BuildJacketCommand());
        command.Subcommands.Add(BuildAudioCommand());
        command.Subcommands.Add(BuildStageCommand());
        command.Subcommands.Add(BuildExtractAfbCommand());
        return command;
    }

    private static Command BuildJacketCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = "Path to the source .mgxc chart."
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = "Path to the output jacket file."
        };
        var assetRootOption = new Option<string?>("--asset-root")
        {
            Description = "Optional directory to scan for additional asset XML before parsing."
        };
        var jacketInputOption = new Option<string?>("--jacket-input")
        {
            Description = "Override the jacket source path instead of using the path from the MGXC metadata."
        };

        var command = new Command("jacket", "Convert the jacket referenced by an MGXC chart.");
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        command.Options.Add(assetRootOption);
        command.Options.Add(jacketInputOption);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var assetRoot = CliPaths.ResolveOptionalPath(parseResult.GetValue(assetRootOption));
            var jacketInput = CliPaths.ResolveOptionalPath(parseResult.GetValue(jacketInputOption));
            var outputFormat = RootCommands.GetOutputFormat(parseResult);

            return await CliOperations.ExecuteAsync("media jacket", outputFormat, async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, assetRoot, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                {
                    return new CliCommandOutcome(parsed.ToResult(), Data: new CliCommandData(InputPath: input, OutputPath: output, AssetRoot: assetRoot));
                }

                var sourcePath = jacketInput ?? parsed.Value.Meta.FullJacketFilePath;
                CliPaths.EnsureParentDirectory(output);
                var converted = await new JacketConverter(
                    new JacketConvertRequest(sourcePath, output),
                    runtime.MediaTool).ConvertAsync(ct);
                var result = CliPaths.Merge(parsed.Diagnostics, converted);
                var data = CliOperations.CreateJacketData(input, output, assetRoot, sourcePath, parsed.Value.Meta);
                var message = result.Succeeded ? $"Wrote jacket: {output}" : null;
                return new CliCommandOutcome(result, message, data);
            }, cancellationToken);
        });

        return command;
    }

    private static Command BuildAudioCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = "Path to the source .mgxc chart."
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = "Base folder for cue file export."
        };
        var assetRootOption = new Option<string?>("--asset-root")
        {
            Description = "Optional directory to scan for additional asset XML before parsing."
        };
        var audioOptions = CommandLineOptions.CreateAudioCommandOptions();

        var command = new Command("audio", "Convert the audio referenced by an MGXC chart into ACB/AWB assets.");
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        command.Options.Add(assetRootOption);
        CommandLineOptions.AddAudioCommandOptions(command, audioOptions);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var assetRoot = CliPaths.ResolveOptionalPath(parseResult.GetValue(assetRootOption));
            var audioOverrides = CommandLineOptions.GetAudioRequestOverrides(parseResult, audioOptions);
            var outputFormat = RootCommands.GetOutputFormat(parseResult);

            return await CliOperations.ExecuteAsync("media audio", outputFormat, async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, assetRoot, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                {
                    return new CliCommandOutcome(parsed.ToResult(), Data: new CliCommandData(InputPath: input, OutputDirectory: output, AssetRoot: assetRoot));
                }

                var converted = await CliOperations.ConvertAudioAsync(runtime, parsed.Value.Meta, output, audioOverrides, ct);
                var result = CliPaths.Merge(parsed.Diagnostics, converted);
                var data = CliOperations.CreateAudioData(input, output, assetRoot, parsed.Value.Meta);
                var message = result.Succeeded ? $"Exported audio assets: {output}" : null;
                return new CliCommandOutcome(result, message, data);
            }, cancellationToken);
        });

        return command;
    }

    private static Command BuildStageCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = "Path to the source .mgxc chart."
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = "Base folder for stage export."
        };
        var assetRootOption = new Option<string?>("--asset-root")
        {
            Description = "Optional directory to scan for additional asset XML before parsing."
        };
        var stageOptions = CommandLineOptions.CreateStageCommandOptions();

        var command = new Command("stage", "Build the custom stage referenced by an MGXC chart.");
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        command.Options.Add(assetRootOption);
        CommandLineOptions.AddStageCommandOptions(command, stageOptions);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var assetRoot = CliPaths.ResolveOptionalPath(parseResult.GetValue(assetRootOption));
            var stageOverrides = CommandLineOptions.GetStageRequestOverrides(parseResult, stageOptions);
            var outputFormat = RootCommands.GetOutputFormat(parseResult);

            return await CliOperations.ExecuteAsync("media stage", outputFormat, async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, assetRoot, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                {
                    return new CliCommandOutcome(parsed.ToResult(), Data: new CliCommandData(InputPath: input, OutputDirectory: output, AssetRoot: assetRoot));
                }

                var built = await CliOperations.BuildStageAsync(runtime, parsed.Value.Meta, output, stageOverrides, ct);
                var result = CliPaths.Merge(parsed.Diagnostics, built.ToResult());
                var data = CliOperations.CreateStageData(input, output, assetRoot, parsed.Value.Meta, stageOverrides);
                var message = result.Succeeded && built.Value is not null ? $"Built stage: {built.Value}" : null;
                return new CliCommandOutcome(result, message, data);
            }, cancellationToken);
        });

        return command;
    }

    private static Command BuildExtractAfbCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = "Path to the source .afb file."
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = "Folder to extract DDS files into."
        };

        var command = new Command("extract-afb", "Extract DDS textures from an AFB archive.");
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var outputFormat = RootCommands.GetOutputFormat(parseResult);

            return await CliOperations.ExecuteAsync("media extract-afb", outputFormat, async (runtime, ct) =>
            {
                var extracted = await new AfbExtractor(new AfbExtractRequest(input, output), runtime.MediaTool).ExtractAsync(ct);
                var data = extracted.Succeeded ? CliOperations.CreateExtractAfbData(input, output) : new CliCommandData(InputPath: input, OutputDirectory: output, SourcePath: input);
                var message = extracted.Succeeded ? $"Extracted DDS files: {output}" : null;
                return new CliCommandOutcome(extracted, message, data);
            }, cancellationToken);
        });

        return command;
    }
}
