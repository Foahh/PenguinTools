using PenguinTools.Chart.Parser;
using PenguinTools.Chart.Writer;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Xml;
using PenguinTools.Infrastructure;
using PenguinTools.Media;

namespace PenguinTools.CLI;

internal static class CliOperations
{
    internal static async Task<int> ExecuteAsync(Func<CliRuntime, CancellationToken, Task<OperationResult>> action, CancellationToken cancellationToken)
    {
        using var runtime = CliRuntime.Create();

        try
        {
            var result = await action(runtime, cancellationToken);
            CliDiagnostics.WriteDiagnostics(result.Diagnostics);
            return result.Succeeded ? 0 : 1;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("error: operation cancelled");
            return 1;
        }
        catch (Exception ex)
        {
            CliDiagnostics.WriteException(ex);
            return 1;
        }
    }

    internal static async Task<OperationResult<PenguinTools.Chart.Models.mgxc.Chart>> ParseChartAsync(
        CliRuntime runtime,
        string input,
        string? assetRoot,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(input))
        {
            return CliPaths.CreateFailureResultOf<PenguinTools.Chart.Models.mgxc.Chart>($"Chart file not found: {input}", input);
        }

        if (!string.IsNullOrWhiteSpace(assetRoot))
        {
            await runtime.Assets.CollectAssetsAsync(assetRoot, cancellationToken);
        }

        return await new MgxcParser(new MgxcParseRequest(input, runtime.Assets), runtime.MediaTool).ParseAsync(cancellationToken);
    }

    internal static async Task<OperationResult> ExportWorkflowAsync(
        CliRuntime runtime,
        PenguinTools.Chart.Models.mgxc.Chart chart,
        string output,
        string? jacketInput,
        MusicRequestOverrides musicOverrides,
        StageRequestOverrides stageOverrides,
        CancellationToken cancellationToken)
    {
        var diagnostics = DiagnosticSnapshot.Empty;
        var meta = chart.Meta;
        var stage = meta.Stage;

        if (ShouldBuildStage(meta, stageOverrides))
        {
            var builtStage = await BuildStageAsync(runtime, meta, output, stageOverrides, cancellationToken);
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
        CliPaths.EnsureParentDirectory(chartPath);

        var writtenChart = await new C2SChartWriter(new C2SWriteRequest(chartPath, chart)).WriteAsync(cancellationToken);
        diagnostics = diagnostics.Merge(writtenChart.Diagnostics);
        if (!writtenChart.Succeeded)
        {
            return OperationResult.Failure().WithDiagnostics(diagnostics);
        }

        var jacketPath = Path.Combine(musicFolder, musicXml.JaketFile);
        var convertedJacket = await new JacketConverter(
            new JacketConvertRequest(jacketInput ?? meta.FullJacketFilePath, jacketPath),
            runtime.MediaTool).ConvertAsync(cancellationToken);
        diagnostics = diagnostics.Merge(convertedJacket.Diagnostics);
        if (!convertedJacket.Succeeded)
        {
            return OperationResult.Failure().WithDiagnostics(diagnostics);
        }

        var convertedMusic = await ConvertMusicAsync(runtime, meta, output, musicOverrides, cancellationToken);
        diagnostics = diagnostics.Merge(convertedMusic.Diagnostics);
        return (convertedMusic.Succeeded ? OperationResult.Success() : OperationResult.Failure()).WithDiagnostics(diagnostics);
    }

    internal static async Task<OperationResult> ConvertMusicAsync(
        CliRuntime runtime,
        Meta meta,
        string output,
        MusicRequestOverrides overrides,
        CancellationToken cancellationToken)
    {
        var request = new MusicConvertRequest(
            meta,
            output,
            overrides.DummyAcbPath ?? runtime.AssetProvider.GetPath(InfrastructureAsset.DummyAcb),
            overrides.WorkingAudioPath ?? runtime.ResourceStore.GetTempPath($"c_{Path.GetFileNameWithoutExtension(meta.FullBgmFilePath)}.wav"),
            overrides.HcaEncryptionKey ?? MusicConvertRequest.DefaultHcaEncryptionKey);

        return await new MusicConverter(request, runtime.MediaTool).ConvertAsync(cancellationToken);
    }

    internal static async Task<OperationResult<Entry>> BuildStageAsync(
        CliRuntime runtime,
        Meta meta,
        string output,
        StageRequestOverrides overrides,
        CancellationToken cancellationToken)
    {
        var backgroundPath = overrides.BackgroundPath ?? meta.FullBgiFilePath;
        if (string.IsNullOrWhiteSpace(backgroundPath))
        {
            return CliPaths.CreateFailureResultOf<Entry>("A background path is required to build a stage.");
        }

        var noteFieldLane = CliPaths.CreateEntry(meta.NotesFieldLine, overrides.NoteFieldLaneId, overrides.NoteFieldLaneName, overrides.NoteFieldLaneData);
        var request = new StageBuildRequest(
            runtime.Assets,
            backgroundPath,
            overrides.EffectPaths,
            overrides.StageId ?? meta.StageId,
            output,
            noteFieldLane,
            overrides.StageTemplatePath ?? runtime.AssetProvider.GetPath(InfrastructureAsset.StageTemplate),
            overrides.NotesFieldTemplatePath ?? runtime.AssetProvider.GetPath(InfrastructureAsset.NotesFieldTemplate));

        return await new StageConverter(request, runtime.MediaTool).BuildAsync(cancellationToken);
    }

    internal static bool ShouldBuildStage(Meta meta, StageRequestOverrides overrides)
    {
        return meta.IsCustomStage || overrides.HasBuildInputs;
    }
}
