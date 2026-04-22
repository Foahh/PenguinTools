using System.CommandLine;
using PenguinTools.Core;
using PenguinTools.Core.Diagnostic;
using PenguinTools.Workflow;

namespace PenguinTools.CLI;

internal static class ScanCommands
{
    internal static Command BuildScanCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = "Directory containing chart files to scan recursively."
        };
        var chartFileDiscoveryOption = CommandLineOptions.CreateChartFileDiscoveryOption(
            "Ordered chart formats to scan, for example [mgxc, ugc, sus] or [ugc, sus].");
        var batchSizeOption = new Option<int>("--batch-size")
        {
            Description = "Maximum scan concurrency.",
            DefaultValueFactory = _ => 8
        };

        var command = new Command("scan",
            "Scan chart files in a folder.");
        command.Arguments.Add(inputArgument);
        command.Options.Add(chartFileDiscoveryOption);
        command.Options.Add(batchSizeOption);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = CliPaths.ResolvePath(parseResult.GetRequiredValue(inputArgument));
            var batchSize = parseResult.GetValue(batchSizeOption);
            var outputOptions = RootCommands.GetOutputOptions(parseResult);

            return await CliOperations.ExecuteAsync("scan", outputOptions, async (runtime, ct) =>
            {
                if (!Directory.Exists(input))
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(
                            CliDiagnostics.SnapshotFromMessage($"Directory not found: {input}")),
                        Data: new CliCommandData(input));

                if (!CommandLineOptions.TryGetChartFileDiscovery(parseResult, chartFileDiscoveryOption,
                        out var chartFileDiscovery, out var error))
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage(error!)),
                        Data: new CliCommandData(input));

                if (batchSize == 0 || batchSize < -1)
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage(
                            "--batch-size must be -1 or a positive integer.")),
                        Data: new CliCommandData(input));

                var resolvedDiscovery = chartFileDiscovery ?? ChartFileDiscoveryFormats.Default;
                var scanned = await CliOperations.ScanOptionChartsAsync(
                    runtime,
                    input,
                    resolvedDiscovery,
                    batchSize,
                    input,
                    ct);

                if (scanned.Value is null)
                    return new CliCommandOutcome(scanned.ToResult(), Data: new CliCommandData(input));

                return new CliCommandOutcome(
                    (scanned.Succeeded ? OperationResult.Success() : OperationResult.Failure())
                    .WithDiagnostics(DiagnosticSnapshot.Empty),
                    Data: CliOperations.CreateScanData(
                        input,
                        scanned.Value,
                        resolvedDiscovery,
                        batchSize,
                        scanned.Diagnostics));
            }, cancellationToken);
        });

        return command;
    }
}
