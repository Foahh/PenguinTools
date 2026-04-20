using System.CommandLine;
using System.Text.Json;
using PenguinTools.Core;
using PenguinTools.Workflow;

namespace PenguinTools.CLI;

internal static class OptionCommands
{
    private const string OptionsFileName = "options.json";

    internal static Command BuildOptionCommand()
    {
        var command = new Command("option",
            "Export an option bundle from scanned charts, using options.json by default.");
        command.Subcommands.Add(BuildOptionConvertCommand());
        return command;
    }

    private static Command BuildOptionConvertCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = "Directory containing chart files; includes options.json when --load-json is true (default)."
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = "Working directory for export."
        };

        var command = new Command("convert",
            "Scan charts and export the option bundle, using options.json by default or CLI configuration when --load-json is false.");
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        var cliOptions = new OptionConvertCliOptions();
        cliOptions.AddTo(command);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var outputFormat = RootCommands.GetOutputFormat(parseResult);

            return await CliOperations.ExecuteAsync("option convert", outputFormat, async (runtime, ct) =>
            {
                var loadJson = parseResult.GetValue(cliOptions.LoadJson);
                OptionDocument json;

                if (loadJson)
                {
                    var optionsPath = Path.Combine(input, OptionsFileName);
                    if (!File.Exists(optionsPath))
                        return new CliCommandOutcome(
                            OperationResult.Failure()
                                .WithDiagnostics(
                                    CliDiagnostics.SnapshotFromMessage($"Missing {OptionsFileName} under: {input}")),
                            $"Expected {OptionsFileName} in the input directory.");

                    await using var optionsStream = File.OpenRead(optionsPath);
                    var fromFile = await JsonSerializer.DeserializeAsync<OptionDocument>(optionsStream,
                        CliJsonSerializerContext.Default.OptionDocument, ct);
                    if (fromFile is null)
                        return new CliCommandOutcome(
                            OperationResult.Failure()
                                .WithDiagnostics(CliDiagnostics.SnapshotFromMessage("Failed to read options.json.")),
                            "Failed to read options.json.");

                    json = fromFile;
                }
                else
                {
                    var cliOptionName = parseResult.GetValue(cliOptions.OptionName);
                    if (string.IsNullOrWhiteSpace(cliOptionName) || cliOptionName.Length != 4)
                        return new CliCommandOutcome(
                            OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage(
                                "With --load-json false, --option-name (exactly four characters) is required.")),
                            "Provide --option-name with exactly four characters when --load-json is false.");

                    json = new OptionDocument();
                }

                if (cliOptions.TryApply(parseResult, json) is { } overrideError)
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage(overrideError)),
                        overrideError);

                if (!json.HasExportableWork())
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage(
                            "No export actions are enabled after applying options.json and command-line overrides.")),
                        "Enable at least one of convert chart, jacket, audio, background, or event XML (in options.json or via CLI overrides).");

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
                        "No charts were found to export.",
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

                var message = result.Succeeded ? $"Exported option bundle: {bundleRoot}" : null;
                return new CliCommandOutcome(
                    result,
                    message,
                    new CliCommandData(input, OutputDirectory: bundleRoot));
            }, cancellationToken);
        });

        return command;
    }

    private sealed class OptionConvertCliOptions
    {
        internal Option<bool> LoadJson { get; } = new("--load-json")
        {
            Description =
                "When true (default), read options.json from the input directory. When false, skip the file and use defaults plus CLI flags; --option-name is then required.",
            DefaultValueFactory = _ => true
        };

        internal Option<string?> OptionName { get; } = new("--option-name")
        {
            Description =
                "Four-letter bundle name. Required when --load-json is false; otherwise overrides options.json optionName."
        };

        internal Option<string?> ChartFileDiscovery { get; } =
            CommandLineOptions.CreateChartFileDiscoveryOption(
                "Override options.json chartFileDiscovery with an ordered list, for example [ugc, mgxc] or ugc,mgxc.");

        internal Option<int?> BatchSize { get; } = new("--batch-size")
        {
            Description = "Override options.json batchSize."
        };

        internal Option<bool?> ConvertChart { get; } = new("--convert-chart")
        {
            Description = "Override options.json convertChart (true or false)."
        };

        internal Option<bool?> ConvertAudio { get; } = new("--convert-audio")
        {
            Description = "Override options.json convertAudio (true or false)."
        };

        internal Option<bool?> ConvertJacket { get; } = new("--convert-jacket")
        {
            Description = "Override options.json convertJacket (true or false)."
        };

        internal Option<bool?> ConvertBackground { get; } = new("--convert-background")
        {
            Description = "Override options.json convertBackground (true or false)."
        };

        internal Option<bool?> GenerateEventXml { get; } = new("--generate-event-xml")
        {
            Description = "Override options.json generateEventXml (true or false)."
        };

        internal Option<bool?> GenerateReleaseTagXml { get; } = new("--generate-release-tag-xml")
        {
            Description = "Override options.json generateReleaseTagXml (true or false)."
        };

        internal Option<int?> UltimaEventId { get; } = new("--ultima-event-id")
        {
            Description = "Override options.json ultimaEventId."
        };

        internal Option<int?> WeEventId { get; } = new("--we-event-id")
        {
            Description = "Override options.json weEventId."
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
            command.Options.Add(UltimaEventId);
            command.Options.Add(WeEventId);
        }

        internal string? TryApply(ParseResult parseResult, OptionDocument document)
        {
            if (parseResult.GetValue(OptionName) is { Length: > 0 } optionName)
            {
                if (optionName.Length != 4) return "--option-name must be exactly four characters.";

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

            if (parseResult.GetValue(UltimaEventId) is { } ultimaEventId) document.UltimaEventId = ultimaEventId;

            if (parseResult.GetValue(WeEventId) is { } weEventId) document.WeEventId = weEventId;

            return null;
        }
    }
}
