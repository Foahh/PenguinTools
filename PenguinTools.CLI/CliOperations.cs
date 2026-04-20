using System.CommandLine;
using PenguinTools.Chart.Parser.mgxc;
using PenguinTools.Chart.Parser.ugc;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Xml;
using PenguinTools.Workflow;
using umgr = PenguinTools.Chart.Models.umgr;

namespace PenguinTools.CLI;

internal static class CliOperations
{
    internal static async Task<int> ExecuteAsync(
        string commandName,
        CliOutputOptions outputOptions,
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

        CliOutput.Write(commandName, outputOptions, outcome);
        return outcome.Result.Succeeded ? 0 : 1;
    }

    internal static CliCommandOutcome CreateParseErrorOutcome(ParseResult parseResult)
    {
        var sink = new DiagnosticCollector();

        foreach (var error in parseResult.Errors) sink.Report(new Diagnostic(Severity.Error, error.Message));

        return new CliCommandOutcome(
            OperationResult.Failure().WithDiagnostics(sink),
            "Command-line parsing failed.");
    }

    internal static async Task<OperationResult<umgr.Chart>> ParseChartAsync(
        CliRuntime runtime,
        string input,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(input))
            return CliPaths.CreateFailureResultOf<umgr.Chart>($"Chart file not found: {input}", input);

        var ext = Path.GetExtension(input);
        if (string.Equals(ext, ".ugc", StringComparison.OrdinalIgnoreCase))
            return await new UgcParser(new UgcParseRequest(input, runtime.Assets), runtime.MediaTool).ParseAsync(
                cancellationToken);

        if (string.Equals(ext, ".mgxc", StringComparison.OrdinalIgnoreCase))
            return await new MgxcParser(new MgxcParseRequest(input, runtime.Assets), runtime.MediaTool).ParseAsync(
                cancellationToken);

        return CliPaths.CreateFailureResultOf<umgr.Chart>(
            $"Expected a .mgxc or .ugc chart file. Got extension: \"{ext}\".",
            input);
    }

    internal static Task<OperationResult<IReadOnlyList<OptionBookSnapshot>>> ScanOptionChartsAsync(
        CliRuntime runtime,
        string directory,
        IReadOnlyList<ChartFileFormat> chartFileDiscovery,
        int batchSize,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var diagnostics = new DiagnosticCollector();
        return ChartScanner.ScanDirectoryAsync(
            runtime.Assets,
            runtime.MediaTool,
            directory,
            chartFileDiscovery,
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
        CancellationToken cancellationToken)
    {
        return OptionExporter.ExportAsync(
            new MusicExportContext(runtime.Assets, runtime.MediaTool, runtime.ResourceStore, runtime.AssetProvider),
            settings,
            outputPaths,
            books,
            diagnosticsWorkingDirectory,
            cancellationToken);
    }

    internal static Task<OperationResult> ExportMusicAsync(
        CliRuntime runtime,
        umgr.Chart chart,
        string output,
        string? jacketInput,
        AudioRequestOverrides audioOverrides,
        StageRequestOverrides stageOverrides,
        CancellationToken cancellationToken)
    {
        return MusicExporter.ExportAsync(
            new MusicExportContext(runtime.Assets, runtime.MediaTool, runtime.ResourceStore, runtime.AssetProvider),
            chart,
            output,
            jacketInput,
            audioOverrides,
            stageOverrides,
            cancellationToken);
    }

    internal static Task<OperationResult> ConvertAudioAsync(
        CliRuntime runtime,
        Meta meta,
        string output,
        AudioRequestOverrides overrides,
        CancellationToken cancellationToken)
    {
        return MusicExporter.ConvertAudioAsync(
            new MusicExportContext(runtime.Assets, runtime.MediaTool, runtime.ResourceStore, runtime.AssetProvider),
            meta,
            output,
            overrides,
            cancellationToken);
    }

    internal static Task<OperationResult<Entry>> BuildStageAsync(
        CliRuntime runtime,
        Meta meta,
        string output,
        StageRequestOverrides overrides,
        CancellationToken cancellationToken)
    {
        return MusicExporter.BuildStageAsync(
            new MusicExportContext(runtime.Assets, runtime.MediaTool, runtime.ResourceStore, runtime.AssetProvider),
            meta,
            output,
            overrides,
            cancellationToken);
    }

    internal static bool ShouldBuildStage(Meta meta, StageRequestOverrides overrides)
    {
        return MusicExporter.ShouldBuildStage(meta, overrides);
    }

    internal static CliChartSummary CreateChartSummary(Meta meta)
    {
        return new CliChartSummary(meta.MgxcId, meta.Id, meta.Title, meta.Difficulty.ToString(), meta.Level);
    }

    internal static CliCommandData CreateScanData(
        string input,
        IReadOnlyList<OptionBookSnapshot> books,
        IReadOnlyList<ChartFileFormat> chartFileDiscovery,
        int batchSize,
        DiagnosticSnapshot diagnostics)
    {
        var orderedDiagnostics = diagnostics.Diagnostics
            .OrderByDescending(d => d.Severity)
            .ThenBy(d => d.Path, StringComparer.Ordinal)
            .ThenBy(d => d.Line)
            .ThenBy(d => d.Time)
            .ThenBy(d => d.Message, StringComparer.Ordinal)
            .ToArray();
        var diagnosticBuckets = orderedDiagnostics
            .Select((diagnostic, index) => new IndexedDiagnostic(index, diagnostic))
            .GroupBy(item => NormalizePath(input, item.Diagnostic.Path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        HashSet<int> matchedDiagnosticIndexes = [];

        var scanBooks = books
            .OrderBy(book => book.BookMeta.Id)
            .ThenBy(book => book.Title, StringComparer.Ordinal)
            .Select(book =>
            {
                var charts = book.Difficulties.Values
                    .OrderBy(item => item.Difficulty)
                    .Select(item => new CliScanDifficultySummary(
                        item.Difficulty.ToString(),
                        item.Meta.MgxcId,
                        item.Meta.Id,
                        item.Meta.Title,
                        item.Meta.Artist,
                        item.Meta.Designer,
                        item.Meta.Level,
                        item.Meta.IsMain,
                        item.Meta.FilePath,
                        GetChartDiagnostics(input, item.Meta.FilePath, diagnosticBuckets, matchedDiagnosticIndexes)))
                    .ToArray();

                var mainDifficulty = book.Difficulties.Values.FirstOrDefault(item => item.Meta.IsMain)?.Difficulty
                                         .ToString() ??
                                     book.Difficulties.Values.OrderByDescending(item => item.Difficulty)
                                         .FirstOrDefault()?.Difficulty.ToString();

                return new CliScanBookSummary(
                    book.BookMeta.Id,
                    book.Title,
                    book.BookMeta.Artist,
                    mainDifficulty,
                    book.IsCustomStage,
                    book.StageId,
                    CreateEntrySummary(book.NotesFieldLine),
                    CreateEntrySummary(book.Stage),
                    charts);
            })
            .ToArray();

        var scan = new CliScanSummary(
            ChartFileDiscoveryFormats.Format(chartFileDiscovery),
            batchSize,
            scanBooks.Length,
            scanBooks.Sum(book => book.Charts.Length),
            scanBooks,
            CliDiagnostics.ToPayload(orderedDiagnostics.Where((_, index) =>
                !matchedDiagnosticIndexes.Contains(index))));

        return new CliCommandData(input, Scan: scan);
    }

    internal static CliCommandData CreateChartConvertData(string input, string output, Meta meta)
    {
        return new CliCommandData(
            input,
            output,
            Chart: CreateChartSummary(meta),
            Artifacts:
            [
                new CliArtifact("chart.c2s", output)
            ]);
    }

    internal static CliCommandData CreateJacketData(string input, string output, string sourcePath, Meta meta)
    {
        return new CliCommandData(
            input,
            output,
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
            input,
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

    internal static CliCommandData CreateStageData(string input, string output, Meta meta,
        StageRequestOverrides overrides)
    {
        var stageId = overrides.StageId ?? meta.StageId;
        var artifacts = new List<CliArtifact>();
        string? stageName = null;

        if (stageId is { } resolvedStageId)
        {
            var xml = new StageXml(resolvedStageId,
                MusicExporter.CreateNoteFieldEntry(meta.NotesFieldLine, overrides.NoteFieldLaneId,
                    overrides.NoteFieldLaneName, overrides.NoteFieldLaneData));
            var stageDirectory = Path.Combine(output, xml.DataName);
            stageName = xml.Name.Str;
            artifacts.Add(new CliArtifact("stage.xml", Path.Combine(stageDirectory, "Stage.xml")));
            artifacts.Add(new CliArtifact("stage.base-afb", Path.Combine(stageDirectory, xml.BaseFile)));
            artifacts.Add(new CliArtifact("stage.notes-field-afb", Path.Combine(stageDirectory, xml.NotesFieldFile)));
        }

        return new CliCommandData(
            input,
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
            artifacts.AddRange(
                Directory.EnumerateFiles(output, "*.dds", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(path => new CliArtifact("dds.file", path)));

        return new CliCommandData(
            input,
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
            var stageXml = new StageXml(resolvedStageId,
                MusicExporter.CreateNoteFieldEntry(meta.NotesFieldLine, stageOverrides.NoteFieldLaneId,
                    stageOverrides.NoteFieldLaneName, stageOverrides.NoteFieldLaneData));
            var stageDirectory = Path.Combine(output, stageXml.DataName);
            stageName = stageXml.Name.Str;
            artifacts.Add(new CliArtifact("stage.xml", Path.Combine(stageDirectory, "Stage.xml")));
            artifacts.Add(new CliArtifact("stage.base-afb", Path.Combine(stageDirectory, stageXml.BaseFile)));
            artifacts.Add(new CliArtifact("stage.notes-field-afb",
                Path.Combine(stageDirectory, stageXml.NotesFieldFile)));
        }

        if (meta is { Difficulty: Difficulty.WorldsEnd or Difficulty.Ultima, UnlockEventId: { } eventId })
        {
            var eventDirectory = Path.Combine(output, $"event{eventId:00000000}");
            artifacts.Add(new CliArtifact("event.xml", Path.Combine(eventDirectory, "Event.xml")));
        }

        return new CliCommandData(
            input,
            OutputDirectory: output,
            SourcePath: jacketInput ?? meta.FullJacketFilePath,
            StageId: stageId,
            StageName: stageName,
            Chart: CreateChartSummary(meta),
            Artifacts: artifacts.ToArray());
    }

    private static DiagnosticSnapshot CreateErrorSnapshot(string message)
    {
        var sink = new DiagnosticCollector();
        sink.Report(new Diagnostic(Severity.Error, message));
        return DiagnosticSnapshot.Create(sink);
    }

    private static CliEntrySummary CreateEntrySummary(Entry entry)
    {
        return new CliEntrySummary(
            entry.Id,
            entry.Str,
            string.IsNullOrWhiteSpace(entry.Data) ? null : entry.Data);
    }

    private static CliDiagnosticPayload[] GetChartDiagnostics(
        string inputRoot,
        string filePath,
        IReadOnlyDictionary<string, IndexedDiagnostic[]> diagnosticBuckets,
        ISet<int> matchedDiagnosticIndexes)
    {
        var keys = new[]
        {
            NormalizePath(inputRoot, filePath),
            NormalizePath(inputRoot, Path.GetFullPath(filePath))
        }.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase);

        List<Diagnostic> matched = [];
        foreach (var key in keys)
        {
            if (!diagnosticBuckets.TryGetValue(key, out var bucket)) continue;

            foreach (var item in bucket)
                if (matchedDiagnosticIndexes.Add(item.Index))
                    matched.Add(item.Diagnostic);
        }

        return CliDiagnostics.ToPayload(matched);
    }

    private static string NormalizePath(string inputRoot, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        var trimmed = path.Trim();
        return Path.IsPathRooted(trimmed)
            ? Path.GetFullPath(trimmed)
            : Path.GetFullPath(Path.Combine(inputRoot, trimmed));
    }

    private sealed record IndexedDiagnostic(int Index, Diagnostic Diagnostic);
}
