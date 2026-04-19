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
        var command = new Command("option", "Export an option bundle from options.json and scanned charts.");
        command.Subcommands.Add(BuildOptionConvertCommand());
        return command;
    }

    private static Command BuildOptionConvertCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = "Directory containing options.json and .mgxc charts."
        };
        var outputArgument = new Argument<string>("output")
        {
            Description = "Working directory for export."
        };

        var command = new Command("convert", "Load options.json, scan charts, and export the option bundle.");
        command.Arguments.Add(inputArgument);
        command.Arguments.Add(outputArgument);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var output = CliPaths.ResolvePath(parseResult.GetRequiredValue(outputArgument));
            var outputFormat = RootCommands.GetOutputFormat(parseResult);

            return await CliOperations.ExecuteAsync("option convert", outputFormat, async (runtime, ct) =>
            {
                var optionsPath = Path.Combine(input, OptionsFileName);
                if (!File.Exists(optionsPath))
                {
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage($"Missing {OptionsFileName} under: {input}")),
                        $"Expected {OptionsFileName} in the input directory.");
                }

                await using var optionsStream = File.OpenRead(optionsPath);
                var json = await JsonSerializer.DeserializeAsync<OptionDocument>(optionsStream, CliJsonSerializerContext.Default.OptionDocument, ct);
                if (json is null)
                {
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage("Failed to read options.json.")),
                        "Failed to read options.json.");
                }

                if (!json.HasExportableWork())
                {
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage("No export actions are enabled in options.json.")),
                        "Enable at least one of convert chart, jacket, audio, background, or event XML in options.json.");
                }

                var scanned = await CliOperations.ScanOptionChartsAsync(
                    runtime,
                    input,
                    json.ChartFileDiscovery,
                    json.BatchSize,
                    output,
                    ct);

                if (!scanned.Succeeded || scanned.Value is null)
                {
                    return new CliCommandOutcome(
                        scanned.ToResult(),
                        Data: new CliCommandData(InputPath: input, OutputDirectory: output));
                }

                if (scanned.Value.Count == 0)
                {
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(scanned.Diagnostics),
                        "No charts were found to export.",
                        new CliCommandData(InputPath: input, OutputDirectory: output));
                }

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
                    new CliCommandData(InputPath: input, OutputDirectory: bundleRoot));
            }, cancellationToken);
        });

        return command;
    }
}
