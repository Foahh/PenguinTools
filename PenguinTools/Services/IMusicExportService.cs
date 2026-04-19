using PenguinTools.Core;
using PenguinTools.Models;

namespace PenguinTools.Services;

public interface IMusicExportService
{
    Task<OperationResult> ExportAsync(MusicModel model, string outputPath, CancellationToken ct);
}