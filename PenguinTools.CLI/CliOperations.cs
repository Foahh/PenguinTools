using PenguinTools.Chart.Parser;
using PenguinTools.Chart.Writer;
using System.CommandLine;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Xml;
using PenguinTools.Infrastructure;
using PenguinTools.Media;

namespace PenguinTools.CLI;

internal static class CliOperations
{
    internal static async Task<int> ExecuteAsync(
        string commandName,
        CliOutputFormat outputFormat,
        Func<CliRuntime, CancellationToken, Task<CliCommandOutcome>> action,
        CancellationToken cancellationToken)
    {
        using var runtime = CliRuntime.Create();
        CliCommandOutcome outcome;

        try
        {
            outcome = await action(runtime, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            outcome = new CliCommandOutcome(
                OperationResult.Failure().WithDiagnostics(CreateErrorSnapshot("operation cancelled")),
                "Operation cancelled.");
        }
        catch (Exception ex)
        {
            outcome = new CliCommandOutcome(
                OperationResult.Failure().WithDiagnostics(CliDiagnostics.SnapshotFromException(ex)),
                ex.Message);
        }

        CliOutput.Write(commandName, outputFormat, outcome);
        return outcome.Result.Succeeded ? 0 : 1;
    }

    internal static CliCommandOutcome CreateParseErrorOutcome(ParseResult parseResult)
    {
        var sink = new Diagnoster();

        foreach (var error in parseResult.Errors)
        {
            sink.Report(Severity.Error, error.Message);
        }

        return new CliCommandOutcome(
            OperationResult.Failure().WithDiagnostics(DiagnosticSnapshot.Create(sink)),
            "Command-line parsing failed.");
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

    internal static CliChartSummary CreateChartSummary(Meta meta)
    {
        return new CliChartSummary(meta.MgxcId, meta.Id, meta.Title, meta.Difficulty.ToString(), meta.Level);
    }

    internal static CliCommandData CreateChartConvertData(string input, string output, string? assetRoot, Meta meta)
    {
        return new CliCommandData(
            InputPath: input,
            OutputPath: output,
            AssetRoot: assetRoot,
            Chart: CreateChartSummary(meta),
            Artifacts:
            [
                new CliArtifact("chart.c2s", output)
            ]);
    }

    internal static CliCommandData CreateJacketData(string input, string output, string? assetRoot, string sourcePath, Meta meta)
    {
        return new CliCommandData(
            InputPath: input,
            OutputPath: output,
            AssetRoot: assetRoot,
            SourcePath: sourcePath,
            Chart: CreateChartSummary(meta),
            Artifacts:
            [
                new CliArtifact("jacket.dds", output)
            ]);
    }

    internal static CliCommandData CreateMusicData(string input, string output, string? assetRoot, Meta meta)
    {
        var songId = meta.Id ?? 0;
        var xml = new CueFileXml(songId);
        var cueDirectory = Path.Combine(output, xml.DataName);

        return new CliCommandData(
            InputPath: input,
            OutputDirectory: output,
            AssetRoot: assetRoot,
            SourcePath: meta.FullBgmFilePath,
            Chart: CreateChartSummary(meta),
            Artifacts:
            [
                new CliArtifact("cue-file.xml", Path.Combine(cueDirectory, "CueFile.xml")),
                new CliArtifact("music.acb", Path.Combine(cueDirectory, xml.AcbFile)),
                new CliArtifact("music.awb", Path.Combine(cueDirectory, xml.AwbFile))
            ]);
    }

    internal static CliCommandData CreateStageData(string input, string output, string? assetRoot, Meta meta, StageRequestOverrides overrides)
    {
        var stageId = overrides.StageId ?? meta.StageId;
        var artifacts = new List<CliArtifact>();
        string? stageName = null;

        if (stageId is { } resolvedStageId)
        {
            var xml = new StageXml(resolvedStageId, CliPaths.CreateEntry(meta.NotesFieldLine, overrides.NoteFieldLaneId, overrides.NoteFieldLaneName, overrides.NoteFieldLaneData));
            var stageDirectory = Path.Combine(output, xml.DataName);
            stageName = xml.Name.Str;
            artifacts.Add(new CliArtifact("stage.xml", Path.Combine(stageDirectory, "Stage.xml")));
            artifacts.Add(new CliArtifact("stage.base-afb", Path.Combine(stageDirectory, xml.BaseFile)));
            artifacts.Add(new CliArtifact("stage.notes-field-afb", Path.Combine(stageDirectory, xml.NotesFieldFile)));
        }

        return new CliCommandData(
            InputPath: input,
            OutputDirectory: output,
            AssetRoot: assetRoot,
            SourcePath: overrides.BackgroundPath ?? meta.FullBgiFilePath,
            StageId: stageId,
            StageName: stageName,
            Chart: CreateChartSummary(meta),
            Artifacts: artifacts);
    }

    internal static CliCommandData CreateExtractAfbData(string input, string output)
    {
        var artifacts = new List<CliArtifact>
        {
            new("dds.directory", output)
        };

        if (Directory.Exists(output))
        {
            artifacts.AddRange(
                Directory.EnumerateFiles(output, "*.dds", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(path => new CliArtifact("dds.file", path)));
        }

        return new CliCommandData(
            InputPath: input,
            OutputDirectory: output,
            SourcePath: input,
            Artifacts: artifacts);
    }

    internal static CliCommandData CreateWorkflowData(
        string input,
        string output,
        string? assetRoot,
        Meta meta,
        string? jacketInput,
        StageRequestOverrides stageOverrides)
    {
        var artifacts = new List<CliArtifact>();
        var metaMap = new Dictionary<Difficulty, Meta>
        {
            [meta.Difficulty] = meta
        };
        var musicXml = new MusicXml(metaMap, meta.Difficulty);
        var musicDirectory = Path.Combine(output, musicXml.DataName);

        artifacts.Add(new CliArtifact("music.xml", Path.Combine(musicDirectory, "Music.xml")));
        artifacts.Add(new CliArtifact("chart.c2s", Path.Combine(musicDirectory, musicXml[meta.Difficulty].File)));
        artifacts.Add(new CliArtifact("jacket.dds", Path.Combine(musicDirectory, musicXml.JaketFile)));

        var songId = meta.Id ?? 0;
        var cueXml = new CueFileXml(songId);
        var cueDirectory = Path.Combine(output, cueXml.DataName);
        artifacts.Add(new CliArtifact("cue-file.xml", Path.Combine(cueDirectory, "CueFile.xml")));
        artifacts.Add(new CliArtifact("music.acb", Path.Combine(cueDirectory, cueXml.AcbFile)));
        artifacts.Add(new CliArtifact("music.awb", Path.Combine(cueDirectory, cueXml.AwbFile)));

        string? stageName = null;
        var stageId = stageOverrides.StageId ?? meta.StageId;
        if (ShouldBuildStage(meta, stageOverrides) && stageId is { } resolvedStageId)
        {
            var stageXml = new StageXml(resolvedStageId, CliPaths.CreateEntry(meta.NotesFieldLine, stageOverrides.NoteFieldLaneId, stageOverrides.NoteFieldLaneName, stageOverrides.NoteFieldLaneData));
            var stageDirectory = Path.Combine(output, stageXml.DataName);
            stageName = stageXml.Name.Str;
            artifacts.Add(new CliArtifact("stage.xml", Path.Combine(stageDirectory, "Stage.xml")));
            artifacts.Add(new CliArtifact("stage.base-afb", Path.Combine(stageDirectory, stageXml.BaseFile)));
            artifacts.Add(new CliArtifact("stage.notes-field-afb", Path.Combine(stageDirectory, stageXml.NotesFieldFile)));
        }

        if (meta is { Difficulty: Difficulty.WorldsEnd or Difficulty.Ultima, UnlockEventId: { } eventId })
        {
            var eventDirectory = Path.Combine(output, $"event{eventId:00000000}");
            artifacts.Add(new CliArtifact("event.xml", Path.Combine(eventDirectory, "Event.xml")));
        }

        return new CliCommandData(
            InputPath: input,
            OutputDirectory: output,
            AssetRoot: assetRoot,
            SourcePath: jacketInput ?? meta.FullJacketFilePath,
            StageId: stageId,
            StageName: stageName,
            Chart: CreateChartSummary(meta),
            Artifacts: artifacts);
    }

    private static DiagnosticSnapshot CreateErrorSnapshot(string message)
    {
        var sink = new Diagnoster();
        sink.Report(Severity.Error, message);
        return DiagnosticSnapshot.Create(sink);
    }
}
