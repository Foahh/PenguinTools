using System.CommandLine;
using PenguinTools.i18n;
using PenguinTools.Media;

namespace PenguinTools.CLI;

internal static class MediaCommands
{
    internal static Command BuildMediaCommand()
    {
        var command = new Command("media", Strings.Cli_Cmd_media);
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
            Description = Strings.Cli_Arg_chart_input
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = Strings.Cli_Arg_jacket_output
        };
        var jacketInputOption = new Option<string?>("--jacket-input")
        {
            Description = Strings.Cli_Arg_jacket_override_chart
        };

        var command = new Command("jacket", Strings.Cli_Cmd_media_jacket);
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        command.Options.Add(jacketInputOption);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var jacketInput = CliPaths.ResolveOptionalPath(parseResult.GetValue(jacketInputOption));
            var outputOptions = RootCommands.GetOutputOptions(parseResult);

            return await CliOperations.ExecuteAsync("media jacket", outputOptions, async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                    return new CliCommandOutcome(parsed.ToResult(), Data: new CliCommandData(input, output));

                var sourcePath = jacketInput ?? parsed.Value.Meta.FullJacketFilePath;
                CliPaths.EnsureParentDirectory(output);
                var converted = await new JacketConverter(
                    new JacketConvertRequest(sourcePath, output),
                    runtime.MediaTool).ConvertAsync(ct);
                var result = CliPaths.Merge(parsed.Diagnostics, converted);
                var data = CliOperations.CreateJacketData(input, output, sourcePath, parsed.Value.Meta);
                var message = result.Succeeded ? string.Format(Strings.Cli_Msg_jacket_written, output) : null;
                return new CliCommandOutcome(result, message, data);
            }, cancellationToken);
        });

        return command;
    }

    private static Command BuildAudioCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = Strings.Cli_Arg_chart_input
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = Strings.Cli_Arg_stage_audio_output
        };
        var audioOptions = CommandLineOptions.CreateAudioCommandOptions();

        var command = new Command("audio", Strings.Cli_Cmd_media_audio);
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        CommandLineOptions.AddAudioCommandOptions(command, audioOptions);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var audioOverrides = CommandLineOptions.GetAudioRequestOverrides(parseResult, audioOptions);
            var outputOptions = RootCommands.GetOutputOptions(parseResult);

            return await CliOperations.ExecuteAsync("media audio", outputOptions, async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                    return new CliCommandOutcome(parsed.ToResult(),
                        Data: new CliCommandData(input, OutputDirectory: output));

                var converted =
                    await CliOperations.ConvertAudioAsync(runtime, parsed.Value.Meta, output, audioOverrides, ct);
                var result = CliPaths.Merge(parsed.Diagnostics, converted);
                var data = CliOperations.CreateAudioData(input, output, parsed.Value.Meta);
                var message = result.Succeeded ? string.Format(Strings.Cli_Msg_audio_exported, output) : null;
                return new CliCommandOutcome(result, message, data);
            }, cancellationToken);
        });

        return command;
    }

    private static Command BuildStageCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = Strings.Cli_Arg_chart_input
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = Strings.Cli_Arg_stage_output
        };
        var stageOptions = CommandLineOptions.CreateStageCommandOptions();

        var command = new Command("stage", Strings.Cli_Cmd_media_stage);
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        CommandLineOptions.AddStageCommandOptions(command, stageOptions);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var stageOverrides = CommandLineOptions.GetStageRequestOverrides(parseResult, stageOptions);
            var outputOptions = RootCommands.GetOutputOptions(parseResult);

            return await CliOperations.ExecuteAsync("media stage", outputOptions, async (runtime, ct) =>
            {
                var parsed = await CliOperations.ParseChartAsync(runtime, input, ct);
                if (!parsed.Succeeded || parsed.Value is null)
                    return new CliCommandOutcome(parsed.ToResult(),
                        Data: new CliCommandData(input, OutputDirectory: output));

                var built = await CliOperations.BuildStageAsync(runtime, parsed.Value.Meta, output, stageOverrides, ct);
                var result = CliPaths.Merge(parsed.Diagnostics, built.ToResult());
                var data = CliOperations.CreateStageData(input, output, parsed.Value.Meta, stageOverrides);
                var message = result.Succeeded && built.Value is not null
                    ? string.Format(Strings.Cli_Msg_built_stage, built.Value)
                    : null;
                return new CliCommandOutcome(result, message, data);
            }, cancellationToken);
        });

        return command;
    }

    private static Command BuildExtractAfbCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = Strings.Cli_Arg_source_afb
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = Strings.Cli_Arg_texture_output
        };

        var command = new Command("extract-afb", Strings.Cli_Cmd_media_extract_afb);
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var outputOptions = RootCommands.GetOutputOptions(parseResult);

            return await CliOperations.ExecuteAsync("media extract-afb", outputOptions, async (runtime, ct) =>
            {
                var extracted = await new AfbExtractor(new AfbExtractRequest(input, output), runtime.MediaTool)
                    .ExtractAsync(ct);
                var data = extracted.Succeeded
                    ? CliOperations.CreateExtractAfbData(input, output)
                    : new CliCommandData(input, OutputDirectory: output, SourcePath: input);
                var message = extracted.Succeeded ? string.Format(Strings.Cli_Msg_extracted_dds, output) : null;
                return new CliCommandOutcome(extracted, message, data);
            }, cancellationToken);
        });

        return command;
    }
}
