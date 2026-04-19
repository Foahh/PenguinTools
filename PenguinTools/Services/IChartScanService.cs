using PenguinTools.Core;
using PenguinTools.Models;
using PenguinTools.Workflow;

namespace PenguinTools.Services;

public sealed record ChartScanParameters(
    string FileGlob,
    IDiagnosticSink Diagnostics,
    int BatchSize,
    string WorkingDirectory,
    ChartFileDiscoveryMode ChartFileDiscovery = ChartFileDiscoveryMode.MgxcOnly);

public interface IChartScanService
{
    Task<OperationResult> ScanAsync(string directory, BookDictionary books, ChartScanParameters parameters, CancellationToken ct);
}
