using System.Collections.Concurrent;
using System.IO;
using PenguinTools.Chart.Writer;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Xml;
using PenguinTools.Infrastructure;
using PenguinTools.Media;
using MStrings = PenguinTools.Media.Resources.Strings;

namespace PenguinTools.Workflow;

public static class OptionExporter
{
    public static async Task<OperationResult> ExportAsync(
        MusicExportContext ctx,
        OptionExportSettings settings,
        ExportOutputPaths outputPaths,
        IEnumerable<OptionBookSnapshot> books,
        string diagnosticsWorkingDirectory,
        CancellationToken ct)
    {
        var diagnostics = OptionExportBatch.CreateDiagnoster();
        var processContext = new OptionExportProcessContext(diagnostics, ct, settings.BatchSize, diagnosticsWorkingDirectory);
        var weEntries = new ConcurrentBag<Entry>();
        var ultEntries = new ConcurrentBag<Entry>();

        var batchDiagnostics = await OptionExportBatch.BatchAsync(
            "convert",
            books,
            (book, innerDiagnostics) => ConvertBookAsync(ctx, book, settings, outputPaths, innerDiagnostics, weEntries, ultEntries, ct),
            book => book.BookMeta.FilePath,
            processContext,
            parallel: true);

        await GenerateAuxiliaryFilesAsync(settings, outputPaths, weEntries, ultEntries, ct);

        return OperationResult.Success().WithDiagnostics(batchDiagnostics.Merge(DiagnosticSnapshot.Create(diagnostics)));
    }

    private static async Task ConvertBookAsync(
        MusicExportContext ctx,
        OptionBookSnapshot book,
        OptionExportSettings settings,
        ExportOutputPaths outputPaths,
        IDiagnosticSink diagnostics,
        ConcurrentBag<Entry> weEntries,
        ConcurrentBag<Entry> ultEntries,
        CancellationToken ct)
    {
        var stage = await BuildStageAsync(ctx, book, settings, outputPaths, diagnostics, ct) ?? book.Stage;
        string? chartFolder = null;
        MusicXml? xml = null;

        if (settings.ConvertChart || settings.ConvertJacket)
        {
            (xml, chartFolder) = await CreateMusicXmlAsync(book, stage, outputPaths.AudioFolder);
        }

        if (settings.ConvertChart && xml is not null && chartFolder is not null)
        {
            await ConvertChartsAsync(book, xml, chartFolder, diagnostics, weEntries, ultEntries, ct);
        }

        if (settings.ConvertJacket && xml is not null && chartFolder is not null)
        {
            await ConvertJacketAsync(book, xml, chartFolder, ctx, diagnostics, ct);
        }

        if (settings.ConvertAudio)
        {
            await ConvertAudioAsync(book, outputPaths.CueFileFolder, ctx, diagnostics, ct);
        }
    }

    private static async Task<Entry?> BuildStageAsync(
        MusicExportContext ctx,
        OptionBookSnapshot book,
        OptionExportSettings settings,
        ExportOutputPaths outputPaths,
        IDiagnosticSink diagnostics,
        CancellationToken ct)
    {
        if (!book.IsCustomStage || !settings.ConvertBackground) return null;
        if (string.IsNullOrWhiteSpace(book.BookMeta.FullBgiFilePath))
            throw new DiagnosticException("Background file path is not set.");
        if (book.StageId is null) throw new DiagnosticException(MStrings.Error_Stage_id_is_not_set);

        var stageConverter = new StageConverter(
            new StageBuildRequest(
                ctx.Assets,
                book.BookMeta.FullBgiFilePath,
                [],
                book.StageId,
                outputPaths.StageFolder,
                book.NotesFieldLine,
                ctx.AssetProvider.GetPath(InfrastructureAsset.StageTemplate),
                ctx.AssetProvider.GetPath(InfrastructureAsset.NotesFieldTemplate)),
            ctx.MediaTool);
        var builtStage = await stageConverter.BuildAsync(ct);
        diagnostics.Report(builtStage.Diagnostics);
        if (!builtStage.Succeeded || builtStage.Value is not { } stageEntry) return null;

        ct.ThrowIfCancellationRequested();
        return stageEntry;
    }

    private static async Task<(MusicXml Xml, string ChartFolder)> CreateMusicXmlAsync(
        OptionBookSnapshot book,
        Entry stage,
        string audioFolder)
    {
        var metaMap = book.Difficulties.ToDictionary(kv => kv.Key, kv => kv.Value.Meta);
        var xml = new MusicXml(metaMap, book.BookMeta.Difficulty)
        {
            StageName = stage
        };

        var chartFolder = await xml.SaveDirectoryAsync(audioFolder);
        return (xml, chartFolder);
    }

    private static void TrackEventEntry(OptionBookSnapshot book, Difficulty difficulty, int songId, ConcurrentBag<Entry> weEntries, ConcurrentBag<Entry> ultEntries)
    {
        if (difficulty == Difficulty.WorldsEnd)
        {
            weEntries.Add(new Entry(songId, book.Title));
        }
        else if (difficulty == Difficulty.Ultima)
        {
            ultEntries.Add(new Entry(songId, book.Title));
        }
    }

    private static async Task ConvertChartsAsync(
        OptionBookSnapshot book,
        MusicXml xml,
        string chartFolder,
        IDiagnosticSink diagnostics,
        ConcurrentBag<Entry> weEntries,
        ConcurrentBag<Entry> ultEntries,
        CancellationToken ct)
    {
        foreach (var (difficulty, item) in book.Difficulties)
        {
            if (item.SongId is not { } songId) throw new DiagnosticException(MStrings.Error_Song_id_is_not_set);

            TrackEventEntry(book, difficulty, songId, weEntries, ultEntries);

            var chartPath = Path.Combine(chartFolder, xml[difficulty].File);
            var chartWriter = new C2SChartWriter(new C2SWriteRequest(chartPath, item.Chart));
            var writtenChart = await chartWriter.WriteAsync(ct);
            diagnostics.Report(writtenChart.Diagnostics);
            if (!writtenChart.Succeeded) return;

            ct.ThrowIfCancellationRequested();
        }
    }

    private static async Task ConvertJacketAsync(
        OptionBookSnapshot book,
        MusicXml xml,
        string chartFolder,
        MusicExportContext ctx,
        IDiagnosticSink diagnostics,
        CancellationToken ct)
    {
        var jacketPath = book.BookMeta.FullJacketFilePath;
        if (!File.Exists(jacketPath))
        {
            diagnostics.Report(Severity.Warning, MStrings.Error_Jacket_file_not_found, target: jacketPath);
            return;
        }

        var jacketConverter = new JacketConverter(
            new JacketConvertRequest(jacketPath, Path.Combine(chartFolder, xml.JaketFile)),
            ctx.MediaTool);
        var convertedJacket = await jacketConverter.ConvertAsync(ct);
        diagnostics.Report(convertedJacket.Diagnostics);
        if (!convertedJacket.Succeeded) return;

        ct.ThrowIfCancellationRequested();
    }

    private static async Task ConvertAudioAsync(
        OptionBookSnapshot book,
        string cueFileFolder,
        MusicExportContext ctx,
        IDiagnosticSink diagnostics,
        CancellationToken ct)
    {
        var audioConverter = new AudioConverter(
            new AudioConvertRequest(
                book.BookMeta,
                cueFileFolder,
                ctx.AssetProvider.GetPath(InfrastructureAsset.DummyAcb),
                ctx.ResourceStore.GetTempPath($"c_{Path.GetFileNameWithoutExtension(book.BookMeta.FullBgmFilePath)}.wav")),
            ctx.MediaTool);
        var convertedAudio = await audioConverter.ConvertAsync(ct);
        diagnostics.Report(convertedAudio.Diagnostics);
        if (!convertedAudio.Succeeded) return;

        ct.ThrowIfCancellationRequested();
    }

    private static async Task GenerateAuxiliaryFilesAsync(
        OptionExportSettings settings,
        ExportOutputPaths outputPaths,
        ConcurrentBag<Entry> weEntries,
        ConcurrentBag<Entry> ultEntries,
        CancellationToken ct)
    {
        if (settings.GenerateReleaseTagXml)
        {
            await ReleaseTag.Default.SaveDirectoryAsync(outputPaths.ReleaseTagPath);
        }

        if (settings.GenerateEventXml && !ultEntries.IsEmpty)
        {
            var eventXml = new EventXml(settings.UltimaEventId, EventXml.MusicType.Ultima, ultEntries.ToHashSet());
            await eventXml.SaveDirectoryAsync(outputPaths.EventFolder);
            ct.ThrowIfCancellationRequested();
        }

        if (settings.GenerateEventXml && !weEntries.IsEmpty)
        {
            var eventXml = new EventXml(settings.WeEventId, EventXml.MusicType.WldEnd, weEntries.ToHashSet());
            await eventXml.SaveDirectoryAsync(outputPaths.EventFolder);
            ct.ThrowIfCancellationRequested();
        }
    }
}
