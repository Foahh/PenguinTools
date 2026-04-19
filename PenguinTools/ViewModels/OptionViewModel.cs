using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Chart.Writer;
using PenguinTools.Core.Chart.Parser;
using PenguinTools.Core.Media;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Resources;
using PenguinTools.Core.Xml;
using PenguinTools.Infrastructure;
using PenguinTools.Models;
using System.Collections.Concurrent;
using System.IO;

namespace PenguinTools.ViewModels;

public partial class OptionViewModel : WatchViewModel<OptionModel>
{
    private const string MgxcExtension = ".mgxc";

    [ObservableProperty]
    public partial Book? SelectedBook { get; set; }

    [ObservableProperty]
    public partial BookItem? SelectedBookItem { get; set; }

    protected override string FileGlob => "*.mgxc";

    protected override bool IsFileChanged(string path) =>
        Model?.Books.Values
            .SelectMany(book => book.Items.Values)
            .Any(item => string.Equals(item.Meta.FilePath, path, StringComparison.OrdinalIgnoreCase)) == true;

    protected override void SetModel(OptionModel? oldModel, OptionModel? newModel)
    {
        base.SetModel(oldModel, newModel);
        SelectedBook = null;
        SelectedBookItem = null;
    }

    protected override async Task<OperationResult<OptionModel>> ReadModel(string path, CancellationToken ct = default)
    {
        var diagnostics = CreateDiagnoster();
        await Dispatcher.InvokeAsync(() =>
        {
            ActionService.Status = Strings.Status_Searching;
            ActionService.StatusTime = DateTime.Now;
        });
        var model = await LoadModelAsync(path, ct);
        var processContext = CreateProcessContext(diagnostics, ct, model);
        var batchDiagnostics = await LoadBooksAsync(path, model.Books, processContext, ct);
        FinalizeBooks(model.Books, diagnostics, ct);

        if (model.Books.Count == 0) throw new DiagnosticException(Strings.Error_No_charts_are_found_directory);
        await Dispatcher.InvokeAsync(() =>
        {
            ActionService.Status = Strings.Status_Done;
            ActionService.StatusTime = DateTime.Now;
        });

        return OperationResult<OptionModel>.Success(model).WithDiagnostics(batchDiagnostics.Merge(DiagnosticSnapshot.Create(diagnostics)));
    }

    protected override async Task<OperationResult> Action(CancellationToken ct = default)
    {
        var diagnostics = CreateDiagnoster();
        var settings = Model;
        if (settings == null) return OperationResult.Success();
        if (!settings.CanExecute) throw new DiagnosticException(Strings.Error_Noop);

        if (settings.Books.Count == 0) throw new DiagnosticException(Strings.Error_No_charts_are_found_directory);

        var workingDirectory = SelectOutputDirectory(settings);
        if (workingDirectory == null) return OperationResult.Success();

        settings.WorkingDirectory = workingDirectory;
        var outputPaths = CreateOutputPaths(settings.OptionDirectory);

        var weEntries = new ConcurrentBag<Entry>();
        var ultEntries = new ConcurrentBag<Entry>();
        await settings.SaveAsync(ModelPath, ct);

        var processContext = CreateProcessContext(diagnostics, ct, settings);
        var exportContext = new ExportContext
        {
            Settings = settings,
            OutputPaths = outputPaths
        };
        var batchDiagnostics = await BatchAsync(
            Strings.Status_Converted,
            settings.Books.Values,
            (book, innerDiagnostics) => ConvertBookAsync(book, exportContext, innerDiagnostics, weEntries, ultEntries, ct),
            book => book.Meta.FilePath,
            processContext,
            parallel: true);

        await GenerateAuxiliaryFilesAsync(exportContext, weEntries, ultEntries, ct);

        return OperationResult.Success().WithDiagnostics(batchDiagnostics.Merge(DiagnosticSnapshot.Create(diagnostics)));
    }

    private static Diagnoster CreateDiagnoster(IDiagnosticSink? parent = null) =>
        new()
        {
            TimeCalculator = parent?.TimeCalculator
        };

    private static ProcessContext CreateProcessContext(IDiagnosticSink diagnostics, CancellationToken ct, OptionModel model) =>
        new()
        {
            Diagnostics = diagnostics,
            CancellationToken = ct,
            BatchSize = model.BatchSize,
            WorkingDirectory = model.WorkingDirectory
        };

    private async Task<OptionModel> LoadModelAsync(string path, CancellationToken ct)
    {
        var model = new OptionModel();
        await model.LoadAsync(path, ct);

        if (string.IsNullOrWhiteSpace(model.WorkingDirectory) || !Directory.Exists(model.WorkingDirectory))
        {
            model.WorkingDirectory = path;
        }

        return model;
    }

    private async Task<DiagnosticSnapshot> LoadBooksAsync(string path, BookDictionary books, ProcessContext context, CancellationToken ct)
    {
        var chartPaths = Directory.EnumerateFiles(path, FileGlob, SearchOption.AllDirectories);
        return await BatchAsync(
            Strings.Status_Checked,
            chartPaths,
            (filePath, innerDiagnostics) => LoadBookAsync(filePath, books, innerDiagnostics, ct),
            filePath => filePath,
            context,
            parallel: true);
    }

    private async Task LoadBookAsync(string filePath, BookDictionary books, IDiagnosticSink diagnostics, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!string.Equals(Path.GetExtension(filePath), MgxcExtension, StringComparison.OrdinalIgnoreCase)) return;

        var parser = new MgxcParser(new MgxcParseRequest(filePath, AssetManager), MediaTool);
        var parsed = await parser.ParseAsync(ct);
        diagnostics.Report(parsed.Diagnostics);
        if (!parsed.Succeeded || parsed.Value is not { } chart) return;

        var meta = chart.Meta;
        var id = meta.Id ?? throw new DiagnosticException(Strings.Error_File_ignored_due_to_id_missing);
        var item = new BookItem(chart);
        Book book;

        lock (books)
        {
            if (!books.TryGetValue(id, out book!))
            {
                book = new Book();
                books[id] = book;
            }
        }

        lock (book)
        {
            if (book.Items.ContainsKey(meta.Difficulty))
            {
                diagnostics.Report(Severity.Warning, Strings.Warn_Duplicate_id_and_difficulty);
            }

            book.Items[meta.Difficulty] = item;
        }
    }

    private static void FinalizeBooks(BookDictionary books, IDiagnosticSink diagnostics, CancellationToken ct)
    {
        foreach (var (id, book) in books.ToArray())
        {
            ct.ThrowIfCancellationRequested();
            var items = book.Items.Values.ToArray();
            if (items.Length == 0)
            {
                books.Remove(id);
                continue;
            }

            if (book.Items.ContainsKey(Difficulty.WorldsEnd) && book.Items.Count != 1)
            {
                diagnostics.Report(Severity.Warning, Strings.Warn_We_chart_must_be_unique_id, target: items);
            }

            var mainItems = items.Where(item => item.Mgxc.Meta.IsMain).ToArray();
            if (mainItems.Length > 1)
            {
                diagnostics.Report(Severity.Warning, Strings.Warn_More_than_one_chart_marked_main, target: mainItems);
            }
            else if (mainItems.Length == 0 && items.Length > 1)
            {
                diagnostics.Report(Severity.Warning, Strings.Warn_No_chart_marked_main, target: items);
            }

            var mainItem = mainItems.FirstOrDefault() ?? items.OrderByDescending(item => item.Difficulty).FirstOrDefault();
            if (mainItem == null)
            {
                books.Remove(id);
                continue;
            }

            book.MainDifficulty = mainItem.Difficulty;
        }
    }

    private string? SelectOutputDirectory(OptionModel settings)
    {
        var dialog = new OpenFolderDialog
        {
            ClientGuid = new Guid("C81454B6-EA09-41D6-90B2-4BD4FB3D5449"),
            Title = Strings.Title_Select_the_output_folder,
            Multiselect = false,
            ValidateNames = true,
            InitialDirectory = GetInitialOutputDirectory(settings)
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    private string GetInitialOutputDirectory(OptionModel settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.WorkingDirectory) && Directory.Exists(settings.WorkingDirectory))
        {
            return settings.WorkingDirectory;
        }

        var modelDirectory = Path.GetDirectoryName(ModelPath);
        return !string.IsNullOrWhiteSpace(modelDirectory) && Directory.Exists(modelDirectory)
            ? modelDirectory
            : string.Empty;
    }

    private static OutputPaths CreateOutputPaths(string rootPath) =>
        new()
        {
            MusicFolder = Path.Combine(rootPath, "music"),
            StageFolder = Path.Combine(rootPath, "stage"),
            CueFileFolder = Path.Combine(rootPath, "cueFile"),
            EventFolder = Path.Combine(rootPath, "event"),
            ReleaseTagPath = Path.Combine(rootPath, "releaseTag")
        };

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
                AssetManager,
                book.Meta.FullBgiFilePath,
                [],
                book.StageId,
                exportContext.OutputPaths.StageFolder,
                book.NotesFieldLine,
                AssetProvider.GetPath(InfrastructureAsset.StageTemplate),
                AssetProvider.GetPath(InfrastructureAsset.NotesFieldTemplate)),
            MediaTool);
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
            MediaTool);
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
                AssetProvider.GetPath(InfrastructureAsset.DummyAcb),
                ResourceStore.GetTempPath($"c_{Path.GetFileNameWithoutExtension(book.Meta.FullBgmFilePath)}.wav")),
            MediaTool);
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

    private static async Task<DiagnosticSnapshot> ProcessItemsAsync<T>(string prefix, IEnumerable<T> items, Func<T, IDiagnosticSink, Task> action, Func<T, string> getPath, ProcessContext main, bool parallel = false)
    {
        var itemList = items as IList<T> ?? [..items];
        var total = itemList.Count;
        var completedCount = 0;
        var diagnostics = new ConcurrentBag<DiagnosticSnapshot>();

        if (parallel)
        {
            await Parallel.ForEachAsync(itemList, new ParallelOptions
            {
                CancellationToken = main.CancellationToken,
                MaxDegreeOfParallelism = main.BatchSize
            }, ProcessItemAsync);
        }
        else
        {
            foreach (var item in itemList) await ProcessItemAsync(item, main.CancellationToken);
        }

        return diagnostics.Aggregate(DiagnosticSnapshot.Empty, (current, snapshot) => current.Merge(snapshot));

        async ValueTask ProcessItemAsync(T item, CancellationToken ct)
        {
            var ld = CreateDiagnoster(main.Diagnostics);
            try
            {
                await action(item, ld);
                ct.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                ld.Report(ex);
            }
            finally
            {
                var done = Interlocked.Increment(ref completedCount);
                diagnostics.Add(CreateItemDiagnostics(ld, getPath(item), main.WorkingDirectory));
            }
        }
    }

    private static DiagnosticSnapshot CreateItemDiagnostics(IDiagnosticSink sink, string path, string workingDirectory)
    {
        var relativePath = Path.GetRelativePath(workingDirectory, path);
        var copied = sink.Diagnostics.Select(diag =>
        {
            var copy = diag.Copy();
            copy.Path ??= relativePath;
            return copy;
        });
        return DiagnosticSnapshot.Create(copied);
    }

    private static Task<DiagnosticSnapshot> BatchAsync<T>(
        string prefix,
        IEnumerable<T> items,
        Func<T, IDiagnosticSink, Task> action,
        Func<T, string> getPath,
        ProcessContext context,
        bool parallel = false) =>
        ProcessItemsAsync(prefix, items, action, getPath, context, parallel);

    protected override Task Reload()
    {
        Model?.SaveAsync(ModelPath);
        return base.Reload();
    }

    private sealed class ProcessContext
    {
        public required IDiagnosticSink Diagnostics { get; init; }
        public required CancellationToken CancellationToken { get; init; }
        public required int BatchSize { get; init; }
        public required string WorkingDirectory { get; init; }
    }

    private sealed class ExportContext
    {
        public required OptionModel Settings { get; init; }
        public required OutputPaths OutputPaths { get; init; }
    }

    private sealed class OutputPaths
    {
        public required string MusicFolder { get; init; }
        public required string StageFolder { get; init; }
        public required string CueFileFolder { get; init; }
        public required string EventFolder { get; init; }
        public required string ReleaseTagPath { get; init; }
    }
}
