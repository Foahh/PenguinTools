using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Infrastructure;
using PenguinTools.Media;
using PenguinTools.Models;
using PenguinTools.Workflow;

namespace PenguinTools.Services;

public sealed class OptionService : IOptionService
{
    private readonly AssetManager _assetManager;
    private readonly IMediaTool _mediaTool;
    private readonly IResourceStore _resourceStore;
    private readonly IInfrastructureAssetProvider _assetProvider;

    public OptionService(
        AssetManager assetManager,
        IMediaTool mediaTool,
        IResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider)
    {
        _assetManager = assetManager;
        _mediaTool = mediaTool;
        _resourceStore = resourceStore;
        _assetProvider = assetProvider;
    }

    public Task<OperationResult> ExportAsync(OptionModel settings, ExportOutputPaths outputPaths, CancellationToken ct)
    {
        var exportSettings = settings.ToDocument().ToExportSettings();

        var snapshots = settings.Books.Values.Select(ToSnapshot).ToArray();
        var ctx = new MusicExportContext(_assetManager, _mediaTool, _resourceStore, _assetProvider);

        return OptionExporter.ExportAsync(ctx, exportSettings, outputPaths, snapshots, settings.WorkingDirectory, ct);
    }

    private static OptionBookSnapshot ToSnapshot(Book book) =>
        new(
            book.Meta,
            book.IsCustomStage,
            book.StageId,
            book.NotesFieldLine,
            book.Stage,
            book.Title,
            book.Items.ToDictionary(kv => kv.Key, kv => new OptionDifficultySnapshot(kv.Value.Difficulty, kv.Value.Id, kv.Value.Mgxc, kv.Value.Meta)));
}
