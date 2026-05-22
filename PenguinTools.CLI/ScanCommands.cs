using System.CommandLine;
using PenguinTools.Core;
using PenguinTools.Core.Diagnostic;
using PenguinTools.i18n;
using PenguinTools.Workflow;

namespace PenguinTools.CLI;

internal static class ScanCommands
{
    internal static Command BuildScanCommand()
    {
        var inputArgument = new Argument<string>("input")
        {
            Description = Strings.Cli_Arg_scan_directory
        };
        var chartFileDiscoveryOption = CommandLineOptions.CreateChartFileDiscoveryOption(
            Strings.Cli_Arg_chart_file_discovery);
        var batchSizeOption = new Option<int>("--batch-size")
        {
            Description = Strings.Cli_Opt_batch_size,
            DefaultValueFactory = _ => 8
        };

        var command = new Command("scan", Strings.Cli_Cmd_scan);
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
                            CliDiagnostics.SnapshotFromMessage(
                                string.Format(Strings.Cli_Msg_directory_not_found, input))),
                        Data: new CliCommandData(input));

                if (!CommandLineOptions.TryGetChartFileDiscovery(parseResult, chartFileDiscoveryOption,
                        out var chartFileDiscovery, out var error))
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage(error!)),
                        Data: new CliCommandData(input));

                if (batchSize == 0 || batchSize < -1)
                    return new CliCommandOutcome(
                        OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromMessage(
                            Strings.Cli_Msg_batch_size_invalid)),
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
