using PenguinTools.Chart.Parser;
using System.CommandLine;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Xml;
using PenguinTools.Infrastructure;
using PenguinTools.Media;
using PenguinTools.Workflow;

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
        CancellationToken cancellationToken)
    {
        if (!File.Exists(input))
        {
            return CliPaths.CreateFailureResultOf<PenguinTools.Chart.Models.mgxc.Chart>($"Chart file not found: {input}", input);
        }

        return await new MgxcParser(new MgxcParseRequest(input, runtime.Assets), runtime.MediaTool).ParseAsync(cancellationToken);
    }

    internal static Task<OperationResult<IReadOnlyList<OptionBookSnapshot>>> ScanOptionChartsAsync(
        CliRuntime runtime,
        string directory,
        string fileGlob,
        int batchSize,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var diagnostics = new Diagnoster();
        return OptionChartScanner.ScanDirectoryAsync(
            runtime.Assets,
            runtime.MediaTool,
            directory,
            fileGlob,
            batchSize,
            workingDirectory,
            diagnostics,
            cancellationToken);
    }

    internal static Task<OperationResult> ExportOptionAsync(
        CliRuntime runtime,
        OptionExportSettings settings,
        ExportOutputPaths outputPaths,
        IReadOnlyList<OptionBookSnapshot> books,
        string diagnosticsWorkingDirectory,
        CancellationToken cancellationToken) =>
        OptionExporter.ExportAsync(
            new MusicExportContext(runtime.Assets, runtime.MediaTool, runtime.ResourceStore, runtime.AssetProvider),
            settings,
            outputPaths,
            books,
            diagnosticsWorkingDirectory,
            cancellationToken);

    internal static Task<OperationResult> ExportMusicAsync(
        CliRuntime runtime,
        PenguinTools.Chart.Models.mgxc.Chart chart,
        string output,
        string? jacketInput,
        AudioRequestOverrides audioOverrides,
        StageRequestOverrides stageOverrides,
        CancellationToken cancellationToken) =>
        MusicExporter.ExportAsync(
            new MusicExportContext(runtime.Assets, runtime.MediaTool, runtime.ResourceStore, runtime.AssetProvider),
            chart,
            output,
            jacketInput,
            audioOverrides,
            stageOverrides,
            cancellationToken);

    internal static Task<OperationResult> ConvertAudioAsync(
        CliRuntime runtime,
        Meta meta,
        string output,
        AudioRequestOverrides overrides,
        CancellationToken cancellationToken) =>
        MusicExporter.ConvertAudioAsync(
            new MusicExportContext(runtime.Assets, runtime.MediaTool, runtime.ResourceStore, runtime.AssetProvider),
            meta,
            output,
            overrides,
            cancellationToken);

    internal static Task<OperationResult<Entry>> BuildStageAsync(
        CliRuntime runtime,
        Meta meta,
        string output,
        StageRequestOverrides overrides,
        CancellationToken cancellationToken) =>
        MusicExporter.BuildStageAsync(
            new MusicExportContext(runtime.Assets, runtime.MediaTool, runtime.ResourceStore, runtime.AssetProvider),
            meta,
            output,
            overrides,
            cancellationToken);

    internal static bool ShouldBuildStage(Meta meta, StageRequestOverrides overrides) =>
        MusicExporter.ShouldBuildStage(meta, overrides);

    internal static CliChartSummary CreateChartSummary(Meta meta)
    {
        return new CliChartSummary(meta.MgxcId, meta.Id, meta.Title, meta.Difficulty.ToString(), meta.Level);
    }

    internal static CliCommandData CreateChartConvertData(string input, string output, Meta meta)
    {
        return new CliCommandData(
            InputPath: input,
            OutputPath: output,
            Chart: CreateChartSummary(meta),
            Artifacts:
            [
                new CliArtifact("chart.c2s", output)
            ]);
    }

    internal static CliCommandData CreateJacketData(string input, string output, string sourcePath, Meta meta)
    {
        return new CliCommandData(
            InputPath: input,
            OutputPath: output,
            SourcePath: sourcePath,
            Chart: CreateChartSummary(meta),
            Artifacts:
            [
                new CliArtifact("jacket.dds", output)
            ]);
    }

    internal static CliCommandData CreateAudioData(string input, string output, Meta meta)
    {
        var songId = meta.Id ?? 0;
        var xml = new CueFileXml(songId);
        var cueDirectory = Path.Combine(output, xml.DataName);

        return new CliCommandData(
            InputPath: input,
            OutputDirectory: output,
            SourcePath: meta.FullBgmFilePath,
            Chart: CreateChartSummary(meta),
            Artifacts:
            [
                new CliArtifact("cue-file.xml", Path.Combine(cueDirectory, "CueFile.xml")),
                new CliArtifact("audio.acb", Path.Combine(cueDirectory, xml.AcbFile)),
                new CliArtifact("audio.awb", Path.Combine(cueDirectory, xml.AwbFile))
            ]);
    }

    internal static CliCommandData CreateStageData(string input, string output, Meta meta, StageRequestOverrides overrides)
    {
        var stageId = overrides.StageId ?? meta.StageId;
        var artifacts = new List<CliArtifact>();
        string? stageName = null;

        if (stageId is { } resolvedStageId)
        {
            var xml = new StageXml(resolvedStageId, MusicExporter.CreateNoteFieldEntry(meta.NotesFieldLine, overrides.NoteFieldLaneId, overrides.NoteFieldLaneName, overrides.NoteFieldLaneData));
            var stageDirectory = Path.Combine(output, xml.DataName);
            stageName = xml.Name.Str;
            artifacts.Add(new CliArtifact("stage.xml", Path.Combine(stageDirectory, "Stage.xml")));
            artifacts.Add(new CliArtifact("stage.base-afb", Path.Combine(stageDirectory, xml.BaseFile)));
            artifacts.Add(new CliArtifact("stage.notes-field-afb", Path.Combine(stageDirectory, xml.NotesFieldFile)));
        }

        return new CliCommandData(
            InputPath: input,
            OutputDirectory: output,
            SourcePath: overrides.BackgroundPath ?? meta.FullBgiFilePath,
            StageId: stageId,
            StageName: stageName,
            Chart: CreateChartSummary(meta),
            Artifacts: artifacts.ToArray());
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
            Artifacts: artifacts.ToArray());
    }

    internal static CliCommandData CreateMusicData(
        string input,
        string output,
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
        var chartBundleDirectory = Path.Combine(output, musicXml.DataName);

        artifacts.Add(new CliArtifact("music.xml", Path.Combine(chartBundleDirectory, "Music.xml")));
        artifacts.Add(new CliArtifact("chart.c2s", Path.Combine(chartBundleDirectory, musicXml[meta.Difficulty].File)));
        artifacts.Add(new CliArtifact("jacket.dds", Path.Combine(chartBundleDirectory, musicXml.JaketFile)));

        var songId = meta.Id ?? 0;
        var cueXml = new CueFileXml(songId);
        var cueDirectory = Path.Combine(output, cueXml.DataName);
        artifacts.Add(new CliArtifact("cue-file.xml", Path.Combine(cueDirectory, "CueFile.xml")));
        artifacts.Add(new CliArtifact("audio.acb", Path.Combine(cueDirectory, cueXml.AcbFile)));
        artifacts.Add(new CliArtifact("audio.awb", Path.Combine(cueDirectory, cueXml.AwbFile)));

        string? stageName = null;
        var stageId = stageOverrides.StageId ?? meta.StageId;
        if (MusicExporter.ShouldBuildStage(meta, stageOverrides) && stageId is { } resolvedStageId)
        {
            var stageXml = new StageXml(resolvedStageId, MusicExporter.CreateNoteFieldEntry(meta.NotesFieldLine, stageOverrides.NoteFieldLaneId, stageOverrides.NoteFieldLaneName, stageOverrides.NoteFieldLaneData));
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
            SourcePath: jacketInput ?? meta.FullJacketFilePath,
            StageId: stageId,
            StageName: stageName,
            Chart: CreateChartSummary(meta),
            Artifacts: artifacts.ToArray());
    }

    private static DiagnosticSnapshot CreateErrorSnapshot(string message)
    {
        var sink = new Diagnoster();
        sink.Report(Severity.Error, message);
        return DiagnosticSnapshot.Create(sink);
    }
}
