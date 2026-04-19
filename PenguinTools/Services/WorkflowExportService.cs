using System.IO;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Chart.Writer;
using PenguinTools.Core.Media;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Resources;
using PenguinTools.Core.Xml;
using PenguinTools.Infrastructure;
using PenguinTools.Models;

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

    public async Task<OperationResult> ExportAsync(WorkflowModel model, string outputPath, CancellationToken ct)
    {
        var diagnostics = DiagnosticSnapshot.Empty;
        var chart = model.Mgxc;
        var meta = chart.Meta;
        var stage = meta.Stage;
        if (meta.IsCustomStage)
        {
            var stageConverter = new StageConverter(
                new StageBuildRequest(
                    _assetManager,
                    meta.FullBgiFilePath,
                    [],
                    meta.StageId,
                    outputPath,
                    meta.NotesFieldLine,
                    _assetProvider.GetPath(InfrastructureAsset.StageTemplate),
                    _assetProvider.GetPath(InfrastructureAsset.NotesFieldTemplate)),
                _mediaTool);
            var builtStage = await stageConverter.BuildAsync(ct);
            diagnostics = diagnostics.Merge(builtStage.Diagnostics);
            if (!builtStage.Succeeded || builtStage.Value is not { } stageEntry) return OperationResult.Failure().WithDiagnostics(diagnostics);
            stage = stageEntry;
        }

        ct.ThrowIfCancellationRequested();
        var metaMap = new Dictionary<Difficulty, Meta>
        {
            [meta.Difficulty] = meta
        };
        var xml = new MusicXml(metaMap, meta.Difficulty)
        {
            StageName = stage
        };

        if (meta is { Difficulty: Difficulty.WorldsEnd or Difficulty.Ultima, UnlockEventId: { } eventId })
        {
            var songId = meta.Id ?? throw new DiagnosticException(Strings.Error_Song_id_is_not_set);
            var type = meta.Difficulty == Difficulty.WorldsEnd ? EventXml.MusicType.WldEnd : EventXml.MusicType.Ultima;
            var eXml = new EventXml(eventId, type, [new Entry(songId, meta.Title)]);
            await eXml.SaveDirectoryAsync(outputPath);
        }

        var musicFolder = await xml.SaveDirectoryAsync(outputPath);
        var chartPath = Path.Combine(musicFolder, xml[meta.Difficulty].File);

        var chartWriter = new C2SChartWriter(new C2SWriteRequest(chartPath, chart));
        var writtenChart = await chartWriter.WriteAsync(ct);
        diagnostics = diagnostics.Merge(writtenChart.Diagnostics);
        if (!writtenChart.Succeeded) return OperationResult.Failure().WithDiagnostics(diagnostics);

        ct.ThrowIfCancellationRequested();

        var jacketConverter = new JacketConverter(
            new JacketConvertRequest(meta.FullJacketFilePath, Path.Combine(musicFolder, xml.JaketFile)),
            _mediaTool);
        var convertedJacket = await jacketConverter.ConvertAsync(ct);
        diagnostics = diagnostics.Merge(convertedJacket.Diagnostics);
        if (!convertedJacket.Succeeded) return OperationResult.Failure().WithDiagnostics(diagnostics);

        ct.ThrowIfCancellationRequested();

        var musicConverter = new MusicConverter(
            new MusicConvertRequest(
                model.Meta,
                outputPath,
                _assetProvider.GetPath(InfrastructureAsset.DummyAcb),
                _resourceStore.GetTempPath($"c_{Path.GetFileNameWithoutExtension(model.Meta.FullBgmFilePath)}.wav")),
            _mediaTool);
        var convertedMusic = await musicConverter.ConvertAsync(ct);
        diagnostics = diagnostics.Merge(convertedMusic.Diagnostics);
        return (convertedMusic.Succeeded ? OperationResult.Success() : OperationResult.Failure()).WithDiagnostics(diagnostics);
    }
}
