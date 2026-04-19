using PenguinTools.Core;
using PenguinTools.Models;

namespace PenguinTools.Services;

public interface IWorkflowExportService
{
    Task<OperationResult> ExportAsync(WorkflowModel model, string outputPath, CancellationToken ct);
}
