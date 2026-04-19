using PenguinTools.Core;
using PenguinTools.Models;

namespace PenguinTools.Services;

public sealed record ChartScanParameters(
    string FileGlob,
    IDiagnosticSink Diagnostics,
    int BatchSize,
    string WorkingDirectory);

public interface IChartScanService
{
    Task<OperationResult> ScanAsync(string directory, BookDictionary books, ChartScanParameters parameters, CancellationToken ct);
}
