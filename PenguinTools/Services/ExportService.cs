using System.Collections.Concurrent;
using System.IO;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Chart.Writer;
using PenguinTools.Media;
using PenguinTools.Core.Metadata;
using PenguinTools.Resources;
using PenguinTools.Core.Xml;
using PenguinTools.Infrastructure;
using PenguinTools.Models;

namespace PenguinTools.Services;

public sealed class ExportService : IExportService
{
    private readonly AssetManager _assetManager;
    private readonly IMediaTool _mediaTool;
    private readonly IEmbeddedResourceStore _resourceStore;
    private readonly IInfrastructureAssetProvider _assetProvider;

    public ExportService(
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

    public async Task<OperationResult> ExportAsync(OptionModel settings, ExportOutputPaths outputPaths, CancellationToken ct)
    {
        var diagnostics = OptionParallelBatch.CreateDiagnoster();
        var processContext = new OptionProcessContext(diagnostics, ct, settings.BatchSize, settings.WorkingDirectory);
        var exportContext = new ExportContext(settings, outputPaths);
        var weEntries = new ConcurrentBag<Entry>();
        var ultEntries = new ConcurrentBag<Entry>();

        var batchDiagnostics = await OptionParallelBatch.BatchAsync(
            Strings.Status_Converted,
            settings.Books.Values,
            (book, innerDiagnostics) => ConvertBookAsync(book, exportContext, innerDiagnostics, weEntries, ultEntries, ct),
            book => book.Meta.FilePath,
            processContext,
            parallel: true);

        await GenerateAuxiliaryFilesAsync(exportContext, weEntries, ultEntries, ct);

        return OperationResult.Success().WithDiagnostics(batchDiagnostics.Merge(DiagnosticSnapshot.Create(diagnostics)));
    }

    private async Task ConvertBookAsync(
        Book book,
        ExportContext exportContext,
        IDiagnosticSink diagnostics,
        ConcurrentBag<Entry> weEntries,
        ConcurrentBag<Entry> ultEntries,
        CancellationToken ct)
    {
        var stage = await BuildStageAsync(book, exportContext, diagnostics, ct) ?? book.Stage;
        string? chartFolder = null;
        MusicXml? xml = null;

        if (exportContext.Settings.ConvertChart || exportContext.Settings.ConvertJacket)
        {
            (xml, chartFolder) = await CreateMusicXmlAsync(book, stage, exportContext.OutputPaths.MusicFolder);
        }

        if (exportContext.Settings.ConvertChart && xml is not null && chartFolder is not null)
        {
            await ConvertChartsAsync(book, xml, chartFolder, diagnostics, weEntries, ultEntries, ct);
        }

        if (exportContext.Settings.ConvertJacket && xml is not null && chartFolder is not null)
        {
            await ConvertJacketAsync(book, xml, chartFolder, diagnostics, ct);
        }

        if (exportContext.Settings.ConvertAudio)
        {
            await ConvertAudioAsync(book, exportContext.OutputPaths.CueFileFolder, diagnostics, ct);
        }
    }

    private async Task<Entry?> BuildStageAsync(Book book, ExportContext exportContext, IDiagnosticSink diagnostics, CancellationToken ct)
    {
        if (!book.IsCustomStage || !exportContext.Settings.ConvertBackground) return null;
        if (string.IsNullOrWhiteSpace(book.Meta.FullBgiFilePath)) throw new DiagnosticException(Strings.Error_Background_file_is_not_set);
        if (book.StageId is null) throw new DiagnosticException(Strings.Error_Stage_id_is_not_set);

        var stageConverter = new StageConverter(
            new StageBuildRequest(
                _assetManager,
                book.Meta.FullBgiFilePath,
                [],
                book.StageId,
                exportContext.OutputPaths.StageFolder,
                book.NotesFieldLine,
                _assetProvider.GetPath(InfrastructureAsset.StageTemplate),
                _assetProvider.GetPath(InfrastructureAsset.NotesFieldTemplate)),
            _mediaTool);
        var builtStage = await stageConverter.BuildAsync(ct);
        diagnostics.Report(builtStage.Diagnostics);
        if (!builtStage.Succeeded || builtStage.Value is not { } stageEntry) return null;

        ct.ThrowIfCancellationRequested();
        return stageEntry;
    }

    private static async Task<(MusicXml Xml, string ChartFolder)> CreateMusicXmlAsync(Book book, Entry stage, string musicFolder)
    {
        var metaMap = book.Items.ToDictionary(item => item.Key, item => item.Value.Meta);
        var xml = new MusicXml(metaMap, book.Difficulty)
        {
            StageName = stage
        };

        var chartFolder = await xml.SaveDirectoryAsync(musicFolder);
        return (xml, chartFolder);
    }

    private static void TrackEventEntry(Book book, Difficulty difficulty, int songId, ConcurrentBag<Entry> weEntries, ConcurrentBag<Entry> ultEntries)
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

    private async Task ConvertChartsAsync(
        Book book,
        MusicXml xml,
        string chartFolder,
        IDiagnosticSink diagnostics,
        ConcurrentBag<Entry> weEntries,
        ConcurrentBag<Entry> ultEntries,
        CancellationToken ct)
    {
        foreach (var (difficulty, item) in book.Items)
        {
            if (item.Id is not { } songId) throw new DiagnosticException(Strings.Error_Song_id_is_not_set);

            TrackEventEntry(book, difficulty, songId, weEntries, ultEntries);

            var chartPath = Path.Combine(chartFolder, xml[item.Difficulty].File);
            var chartWriter = new C2SChartWriter(new C2SWriteRequest(chartPath, item.Mgxc));
            var writtenChart = await chartWriter.WriteAsync(ct);
            diagnostics.Report(writtenChart.Diagnostics);
            if (!writtenChart.Succeeded) return;

            ct.ThrowIfCancellationRequested();
        }
    }

    private async Task ConvertJacketAsync(
        Book book,
        MusicXml xml,
        string chartFolder,
        IDiagnosticSink diagnostics,
        CancellationToken ct)
    {
        var jacketPath = book.Meta.FullJacketFilePath;
        if (!File.Exists(jacketPath))
        {
            diagnostics.Report(Severity.Warning, Strings.Error_Jacket_file_not_found, target: jacketPath);
            return;
        }

        var jacketConverter = new JacketConverter(
            new JacketConvertRequest(jacketPath, Path.Combine(chartFolder, xml.JaketFile)),
            _mediaTool);
        var convertedJacket = await jacketConverter.ConvertAsync(ct);
        diagnostics.Report(convertedJacket.Diagnostics);
        if (!convertedJacket.Succeeded) return;

        ct.ThrowIfCancellationRequested();
    }

    private async Task ConvertAudioAsync(Book book, string cueFileFolder, IDiagnosticSink diagnostics, CancellationToken ct)
    {
        var musicConverter = new MusicConverter(
            new MusicConvertRequest(
                book.Meta,
                cueFileFolder,
                _assetProvider.GetPath(InfrastructureAsset.DummyAcb),
                _resourceStore.GetTempPath($"c_{Path.GetFileNameWithoutExtension(book.Meta.FullBgmFilePath)}.wav")),
            _mediaTool);
        var convertedMusic = await musicConverter.ConvertAsync(ct);
        diagnostics.Report(convertedMusic.Diagnostics);
        if (!convertedMusic.Succeeded) return;

        ct.ThrowIfCancellationRequested();
    }

    private static async Task GenerateAuxiliaryFilesAsync(
        ExportContext exportContext,
        ConcurrentBag<Entry> weEntries,
        ConcurrentBag<Entry> ultEntries,
        CancellationToken ct)
    {
        if (exportContext.Settings.GenerateReleaseTagXml)
        {
            await ReleaseTag.Default.SaveDirectoryAsync(exportContext.OutputPaths.ReleaseTagPath);
        }

        if (exportContext.Settings.GenerateEventXml && !ultEntries.IsEmpty)
        {
            var eventXml = new EventXml(exportContext.Settings.UltimaEventId, EventXml.MusicType.Ultima, ultEntries.ToHashSet());
            await eventXml.SaveDirectoryAsync(exportContext.OutputPaths.EventFolder);
            ct.ThrowIfCancellationRequested();
        }

        if (exportContext.Settings.GenerateEventXml && !weEntries.IsEmpty)
        {
            var eventXml = new EventXml(exportContext.Settings.WeEventId, EventXml.MusicType.WldEnd, weEntries.ToHashSet());
            await eventXml.SaveDirectoryAsync(exportContext.OutputPaths.EventFolder);
            ct.ThrowIfCancellationRequested();
        }
    }

    private sealed class ExportContext
    {
        public ExportContext(OptionModel settings, ExportOutputPaths outputPaths)
        {
            Settings = settings;
            OutputPaths = outputPaths;
        }

        public OptionModel Settings { get; }
        public ExportOutputPaths OutputPaths { get; }
    }
}
