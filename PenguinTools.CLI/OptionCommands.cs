using System.CommandLine;
using System.Text.Json;
using PenguinTools.Core;
using PenguinTools.Core.IO;
using PenguinTools.i18n;
using PenguinTools.Workflow;

namespace PenguinTools.CLI;

internal static class OptionCommands
{
    private const string OptionsFileName = "options.json";

    internal static Command BuildOptionCommand()
    {
        var command = new Command("option", Strings.Cli_Cmd_option);
        command.Subcommands.Add(BuildOptionConvertCommand());
        return command;
    }

    private static Command BuildOptionConvertCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = Strings.Cli_Arg_input_directory
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = Strings.Cli_Arg_output_directory
        };

        var command = new Command("convert", Strings.Cli_Cmd_option_convert);
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        var cliOptions = new OptionConvertCliOptions();
        cliOptions.AddTo(command);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var outputOptions = RootCommands.GetOutputOptions(parseResult);

            return await CliOperations.ExecuteAsync("option convert", outputOptions, async (runtime, ct) =>
            {
                var loadJson = parseResult.GetValue(cliOptions.LoadJson);
                OptionDocument json;
                string? optionsPath = null;

                if (loadJson)
                {
                    optionsPath = Path.Combine(input, OptionsFileName);
                    if (!File.Exists(optionsPath))
                        return new CliCommandOutcome(
                            OperationResult.Failure()
                                .WithDiagnostics(
                                    CliDiagnostics.SnapshotFromMessage(
                                        string.Format(Strings.Cli_Msg_missing_options_json, OptionsFileName, input))),
                            string.Format(Strings.Cli_Msg_expected_options_json, OptionsFileName));

                    await using var optionsStream = File.OpenRead(optionsPath);
                    var fromFile = await JsonSerializer.DeserializeAsync<OptionDocument>(optionsStream,
                        CliJsonSerializerContext.Default.OptionDocument, ct);
                    if (fromFile is null)
                        return new CliCommandOutcome(
                            OperationResult.Failure()
                                .WithDiagnostics(
                                    CliDiagnostics.SnapshotFromMessage(Strings.Cli_Msg_failed_read_options_json)),
                            Strings.Cli_Msg_failed_read_options_json);

                    json = fromFile;
                }
                else
                {
                    var cliOptionName = parseResult.GetValue(cliOptions.OptionName);
                    if (string.IsNullOrWhiteSpace(cliOptionName) || cliOptionName.Length != 4)
                        return new CliCommandOutcome(
                            OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage(
                                Strings.Cli_Msg_option_name_required_flag)),
                            Strings.Cli_Msg_option_name_required);

                    json = new OptionDocument();
                }

                if (cliOptions.TryApply(parseResult, json) is { } overrideError)
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage(overrideError)),
                        overrideError);

                if (!json.HasExportableWork())
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage(
                            Strings.Cli_Msg_no_export_actions_enabled)),
                        Strings.Cli_Msg_enable_export_action);

                var scanned = await CliOperations.ScanOptionChartsAsync(
                    runtime,
                    input,
                    json.ChartFileDiscovery,
                    json.BatchSize,
                    output,
                    ct);

                if (!scanned.Succeeded || scanned.Value is null)
                    return new CliCommandOutcome(
                        scanned.ToResult(),
                        Data: new CliCommandData(input, OutputDirectory: output));

                if (scanned.Value.Count == 0)
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(scanned.Diagnostics),
                        Strings.Cli_Msg_no_charts_to_export,
                        new CliCommandData(input, OutputDirectory: output));

                var exportSettings = json.ToExportSettings();
                var bundleRoot = ExportOutputPaths.ResolveBundleRootPath(output, json.OptionName);
                var outputPaths = ExportOutputPaths.FromOptionDirectory(bundleRoot);

                var exported = await CliOperations.ExportOptionAsync(
                    runtime,
                    exportSettings,
                    outputPaths,
                    scanned.Value,
                    output,
                    ct);

                var result = CliPaths.Merge(scanned.Diagnostics, exported);
                if (result.Succeeded && optionsPath is not null) await SaveOptionsJsonAsync(optionsPath, json, ct);

                var message = result.Succeeded
                    ? string.Format(Strings.Cli_Msg_exported_option_bundle, bundleRoot)
                    : null;
                return new CliCommandOutcome(
                    result,
                    message,
                    new CliCommandData(input, OutputDirectory: bundleRoot));
            }, cancellationToken);
        });

        return command;
    }

    private static async Task SaveOptionsJsonAsync(string path, OptionDocument document, CancellationToken ct)
    {
        await AtomicFile.WriteAsync(
            path,
            (stream, cancellationToken) => JsonSerializer.SerializeAsync(
                stream,
                document,
                CliJsonSerializerContext.Default.OptionDocument,
                cancellationToken),
            ct);
    }

    private sealed class OptionConvertCliOptions
    {
        internal Option<bool> LoadJson { get; } = new("--load-json")
        {
            Description = Strings.Cli_Opt_load_json,
            DefaultValueFactory = _ => true
        };

        internal Option<string?> OptionName { get; } = new("--option-name")
        {
            Description = Strings.Cli_Opt_option_name
        };

        internal Option<string?> ChartFileDiscovery { get; } =
            CommandLineOptions.CreateChartFileDiscoveryOption(Strings.Cli_Opt_chart_file_discovery);

        internal Option<int?> BatchSize { get; } = new("--batch-size")
        {
            Description = Strings.Cli_Opt_json_batch_size
        };

        internal Option<bool?> ConvertChart { get; } = new("--convert-chart")
        {
            Description = Strings.Cli_Opt_convert_chart
        };

        internal Option<bool?> ConvertAudio { get; } = new("--convert-audio")
        {
            Description = Strings.Cli_Opt_convert_audio
        };

        internal Option<bool?> ConvertJacket { get; } = new("--convert-jacket")
        {
            Description = Strings.Cli_Opt_convert_jacket
        };

        internal Option<bool?> ConvertBackground { get; } = new("--convert-background")
        {
            Description = Strings.Cli_Opt_convert_background
        };

        internal Option<bool?> GenerateEventXml { get; } = new("--generate-event-xml")
        {
            Description = Strings.Cli_Opt_generate_event_xml
        };

        internal Option<bool?> GenerateReleaseTagXml { get; } = new("--generate-release-tag-xml")
        {
            Description = Strings.Cli_Opt_generate_release_tag_xml
        };

        internal Option<int?> ReleaseTagId { get; } = new("--release-tag-id")
        {
            Description = Strings.Cli_Opt_release_tag_id
        };

        internal Option<string?> ReleaseTagTitleName { get; } = new("--release-tag-title-name")
        {
            Description = Strings.Cli_Opt_release_tag_title_name
        };

        internal Option<int?> UltimaEventId { get; } = new("--ultima-event-id")
        {
            Description = Strings.Cli_Opt_ultima_event_id
        };

        internal Option<int?> WeEventId { get; } = new("--we-event-id")
        {
            Description = Strings.Cli_Opt_we_event_id
        };

        internal void AddTo(Command command)
        {
            command.Options.Add(LoadJson);
            command.Options.Add(OptionName);
            command.Options.Add(ChartFileDiscovery);
            command.Options.Add(BatchSize);
            command.Options.Add(ConvertChart);
            command.Options.Add(ConvertAudio);
            command.Options.Add(ConvertJacket);
            command.Options.Add(ConvertBackground);
            command.Options.Add(GenerateEventXml);
            command.Options.Add(GenerateReleaseTagXml);
            command.Options.Add(ReleaseTagId);
            command.Options.Add(ReleaseTagTitleName);
            command.Options.Add(UltimaEventId);
            command.Options.Add(WeEventId);
        }

        internal string? TryApply(ParseResult parseResult, OptionDocument document)
        {
            if (parseResult.GetValue(OptionName) is { Length: > 0 } optionName)
            {
                if (optionName.Length != 4) return Strings.Error_Option_name_four_characters;

                document.OptionName = optionName;
            }

            if (!CommandLineOptions.TryGetChartFileDiscovery(parseResult, ChartFileDiscovery, out var discovery,
                    out var error))
                return error;

            if (discovery is not null)
                document.ChartFileDiscovery = [.. discovery];

            if (parseResult.GetValue(BatchSize) is { } batchSize) document.BatchSize = batchSize;

            if (parseResult.GetValue(ConvertChart) is { } convertChart) document.ConvertChart = convertChart;

            if (parseResult.GetValue(ConvertAudio) is { } convertAudio) document.ConvertAudio = convertAudio;

            if (parseResult.GetValue(ConvertJacket) is { } convertJacket) document.ConvertJacket = convertJacket;

            if (parseResult.GetValue(ConvertBackground) is { } convertBackground)
                document.ConvertBackground = convertBackground;

            if (parseResult.GetValue(GenerateEventXml) is { } generateEventXml)
                document.GenerateEventXml = generateEventXml;

            if (parseResult.GetValue(GenerateReleaseTagXml) is { } generateReleaseTagXml)
                document.GenerateReleaseTagXml = generateReleaseTagXml;

            if (parseResult.GetValue(ReleaseTagId) is { } releaseTagId) document.ReleaseTagId = releaseTagId;

            if (parseResult.GetValue(ReleaseTagTitleName) is { Length: > 0 } releaseTagTitleName)
                document.ReleaseTagTitleName = releaseTagTitleName;

            if (parseResult.GetValue(UltimaEventId) is { } ultimaEventId) document.UltimaEventId = ultimaEventId;

            if (parseResult.GetValue(WeEventId) is { } weEventId) document.WeEventId = weEventId;

            return null;
        }
    }
}
