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
        command.Subcommands.Add(BuildMusicCommand());
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

            return await CliOperations.ExecuteAsync(async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, assetRoot, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                {
                    return parsed.ToResult();
                }

                CliPaths.EnsureParentDirectory(output);
                var converted = await new JacketConverter(
                    new JacketConvertRequest(jacketInput ?? parsed.Value.Meta.FullJacketFilePath, output),
                    runtime.MediaTool).ConvertAsync(ct);
                var result = CliPaths.Merge(parsed.Diagnostics, converted);

                if (result.Succeeded)
                {
                    Console.WriteLine($"Wrote jacket: {output}");
                }

                return result;
            }, cancellationToken);
        });

        return command;
    }

    private static Command BuildMusicCommand()
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
        var musicOptions = CommandLineOptions.CreateMusicCommandOptions();

        var command = new Command("music", "Convert the music referenced by an MGXC chart into ACB/AWB assets.");
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        command.Options.Add(assetRootOption);
        CommandLineOptions.AddMusicCommandOptions(command, musicOptions);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var assetRoot = CliPaths.ResolveOptionalPath(parseResult.GetValue(assetRootOption));
            var musicOverrides = CommandLineOptions.GetMusicRequestOverrides(parseResult, musicOptions);

            return await CliOperations.ExecuteAsync(async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, assetRoot, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                {
                    return parsed.ToResult();
                }

                var converted = await CliOperations.ConvertMusicAsync(runtime, parsed.Value.Meta, output, musicOverrides, ct);
                var result = CliPaths.Merge(parsed.Diagnostics, converted);

                if (result.Succeeded)
                {
                    Console.WriteLine($"Exported music assets: {output}");
                }

                return result;
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

            return await CliOperations.ExecuteAsync(async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, assetRoot, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                {
                    return parsed.ToResult();
                }

                var built = await CliOperations.BuildStageAsync(runtime, parsed.Value.Meta, output, stageOverrides, ct);
                var result = CliPaths.Merge(parsed.Diagnostics, built.ToResult());

                if (result.Succeeded && built.Value is not null)
                {
                    Console.WriteLine($"Built stage: {built.Value}");
                }

                return result;
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

            return await CliOperations.ExecuteAsync(async (runtime, ct) =>
            {
                var extracted = await new AfbExtractor(new AfbExtractRequest(input, output), runtime.MediaTool).ExtractAsync(ct);
                if (extracted.Succeeded)
                {
                    Console.WriteLine($"Extracted DDS files: {output}");
                }

                return extracted;
            }, cancellationToken);
        });

        return command;
    }
}
