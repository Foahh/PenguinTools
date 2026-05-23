using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Infrastructure;
using PenguinTools.Media;
using PenguinTools.Models;
using PenguinTools.Workflow;

namespace PenguinTools.Services;

public sealed class MusicExportService : IMusicExportService
{
    private readonly AssetManager _assetManager;
    private readonly ActionService _actionService;
    private readonly IInfrastructureAssetProvider _assetProvider;
    private readonly IMediaTool _mediaTool;
    private readonly IResourceStore _resourceStore;

    public MusicExportService(
        AssetManager assetManager,
        IMediaTool mediaTool,
        IResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        ActionService actionService)
    {
        _assetManager = assetManager;
        _mediaTool = mediaTool;
        _resourceStore = resourceStore;
        _assetProvider = assetProvider;
        _actionService = actionService;
    }

    public Task<OperationResult> ExportAsync(MusicModel model, string outputPath, CancellationToken ct)
    {
        var ctx = new MusicExportContext(_assetManager, _mediaTool, _resourceStore, _assetProvider);
        return MusicExporter.ExportAsync(
            ctx,
            model.Mgxc,
            outputPath,
            null,
            AudioRequestOverrides.Default with { HcaEncryptionKey = model.HcaEncryptionKey },
            StageRequestOverrides.None,
            ct,
            _actionService);
    }
}
