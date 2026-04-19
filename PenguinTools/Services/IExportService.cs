using System.IO;
using PenguinTools.Core;
using PenguinTools.Models;

namespace PenguinTools.Services;

public sealed record ExportOutputPaths(
    string MusicFolder,
    string StageFolder,
    string CueFileFolder,
    string EventFolder,
    string ReleaseTagPath)
{
    public static ExportOutputPaths FromOptionDirectory(string rootPath) =>
        new(
            Path.Combine(rootPath, "music"),
            Path.Combine(rootPath, "stage"),
            Path.Combine(rootPath, "cueFile"),
            Path.Combine(rootPath, "event"),
            Path.Combine(rootPath, "releaseTag"));
}

public interface IExportService
{
    Task<OperationResult> ExportAsync(OptionModel settings, ExportOutputPaths outputPaths, CancellationToken ct);
}
