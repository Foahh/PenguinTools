using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Infrastructure;
using PenguinTools.Media;
using PenguinTools.Models;
using PenguinTools.Workflow;

namespace PenguinTools.Services;

public sealed class WorkflowExportService : IWorkflowExportService
{
    private readonly AssetManager _assetManager;
    private readonly IMediaTool _mediaTool;
    private readonly IEmbeddedResourceStore _resourceStore;
    private readonly IInfrastructureAssetProvider _assetProvider;

    public WorkflowExportService(
        AssetManager assetManager,
        IMediaTool mediaTool,
        IEmbeddedResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider)
    {
        _assetManager = assetManager;
        _mediaTool = mediaTool;
        _resourceStore = resourceStore;
        _assetProvider = assetProvider;
    }

    public Task<OperationResult> ExportAsync(WorkflowModel model, string outputPath, CancellationToken ct)
    {
        var ctx = new WorkflowExportContext(_assetManager, _mediaTool, _resourceStore, _assetProvider);
        return WorkflowExporter.ExportAsync(
            ctx,
            model.Mgxc,
            outputPath,
            jacketInput: null,
            MusicRequestOverrides.Default,
            StageRequestOverrides.None,
            ct);
    }
}
