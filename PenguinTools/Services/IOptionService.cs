using PenguinTools.Core;
using PenguinTools.Models;
using PenguinTools.Workflow;

namespace PenguinTools.Services;

public interface IOptionService
{
    Task<OperationResult> ExportAsync(OptionModel settings, ExportOutputPaths outputPaths, CancellationToken ct);
}
