using PenguinTools.Chart.Writer;
using mgxc = PenguinTools.Chart.Models.mgxc;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Xml;
using PenguinTools.Infrastructure;
using PenguinTools.Media;

namespace PenguinTools.Workflow;

public static class WorkflowExporter
{
    public static Entry CreateNoteFieldEntry(Entry current, int? id, string? name, string? data) =>
        WorkflowPaths.CreateEntry(current, id, name, data);

    public static bool ShouldBuildStage(Meta meta, StageRequestOverrides overrides) =>
        meta.IsCustomStage || overrides.HasBuildInputs;

    public static async Task<OperationResult<Entry>> BuildStageAsync(
        WorkflowExportContext ctx,
        Meta meta,
        string output,
        StageRequestOverrides overrides,
        CancellationToken cancellationToken)
    {
        var backgroundPath = overrides.BackgroundPath ?? meta.FullBgiFilePath;
        if (string.IsNullOrWhiteSpace(backgroundPath))
        {
            return WorkflowPaths.CreateFailureResultOf<Entry>("A background path is required to build a stage.");
        }

        var noteFieldLane = WorkflowPaths.CreateEntry(
            meta.NotesFieldLine,
            overrides.NoteFieldLaneId,
            overrides.NoteFieldLaneName,
            overrides.NoteFieldLaneData);
        var request = new StageBuildRequest(
            ctx.Assets,
            backgroundPath,
            overrides.EffectPaths,
            overrides.StageId ?? meta.StageId,
            output,
            noteFieldLane,
            overrides.StageTemplatePath ?? ctx.AssetProvider.GetPath(InfrastructureAsset.StageTemplate),
            overrides.NotesFieldTemplatePath ?? ctx.AssetProvider.GetPath(InfrastructureAsset.NotesFieldTemplate));

        return await new StageConverter(request, ctx.MediaTool).BuildAsync(cancellationToken);
    }

    public static async Task<OperationResult> ConvertMusicAsync(
        WorkflowExportContext ctx,
        Meta meta,
        string output,
        MusicRequestOverrides overrides,
        CancellationToken cancellationToken)
    {
        var request = new MusicConvertRequest(
            meta,
            output,
            overrides.DummyAcbPath ?? ctx.AssetProvider.GetPath(InfrastructureAsset.DummyAcb),
            overrides.WorkingAudioPath ?? ctx.ResourceStore.GetTempPath($"c_{Path.GetFileNameWithoutExtension(meta.FullBgmFilePath)}.wav"),
            overrides.HcaEncryptionKey ?? MusicConvertRequest.DefaultHcaEncryptionKey);

        return await new MusicConverter(request, ctx.MediaTool).ConvertAsync(cancellationToken);
    }

    public static async Task<OperationResult> ExportAsync(
        WorkflowExportContext ctx,
        mgxc.Chart chart,
        string output,
        string? jacketInput,
        MusicRequestOverrides musicOverrides,
        StageRequestOverrides stageOverrides,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = DiagnosticSnapshot.Empty;
        var meta = chart.Meta;
        var stage = meta.Stage;

        if (ShouldBuildStage(meta, stageOverrides))
        {
            var builtStage = await BuildStageAsync(ctx, meta, output, stageOverrides, cancellationToken);
            diagnostics = diagnostics.Merge(builtStage.Diagnostics);
            if (!builtStage.Succeeded || builtStage.Value is null)
            {
                return OperationResult.Failure().WithDiagnostics(diagnostics);
            }

            stage = builtStage.Value;
        }

        if (meta is { Difficulty: Difficulty.WorldsEnd or Difficulty.Ultima, UnlockEventId: { } eventId })
        {
            var songId = meta.Id ?? 0;
            var type = meta.Difficulty == Difficulty.WorldsEnd ? EventXml.MusicType.WldEnd : EventXml.MusicType.Ultima;
            var eventXml = new EventXml(eventId, type, [new Entry(songId, meta.Title)]);
            await eventXml.SaveDirectoryAsync(output);
        }

        var metaMap = new Dictionary<Difficulty, Meta>
        {
            [meta.Difficulty] = meta
        };

        var musicXml = new MusicXml(metaMap, meta.Difficulty)
        {
            StageName = stage
        };

        var musicFolder = await musicXml.SaveDirectoryAsync(output);
        var chartPath = Path.Combine(musicFolder, musicXml[meta.Difficulty].File);
        WorkflowPaths.EnsureParentDirectory(chartPath);

        var writtenChart = await new C2SChartWriter(new C2SWriteRequest(chartPath, chart)).WriteAsync(cancellationToken);
        diagnostics = diagnostics.Merge(writtenChart.Diagnostics);
        if (!writtenChart.Succeeded)
        {
            return OperationResult.Failure().WithDiagnostics(diagnostics);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var jacketPath = Path.Combine(musicFolder, musicXml.JaketFile);
        var convertedJacket = await new JacketConverter(
            new JacketConvertRequest(jacketInput ?? meta.FullJacketFilePath, jacketPath),
            ctx.MediaTool).ConvertAsync(cancellationToken);
        diagnostics = diagnostics.Merge(convertedJacket.Diagnostics);
        if (!convertedJacket.Succeeded)
        {
            return OperationResult.Failure().WithDiagnostics(diagnostics);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var convertedMusic = await ConvertMusicAsync(ctx, meta, output, musicOverrides, cancellationToken);
        diagnostics = diagnostics.Merge(convertedMusic.Diagnostics);
        return (convertedMusic.Succeeded ? OperationResult.Success() : OperationResult.Failure()).WithDiagnostics(diagnostics);
    }
}
