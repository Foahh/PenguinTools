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
using PenguinTools.Models;
using System.Collections.Concurrent;
using System.IO;

namespace PenguinTools.ViewModels;

public partial class OptionViewModel : WatchViewModel<OptionModel>
{
    [ObservableProperty]
    public partial Book? SelectedBook { get; set; }

    [ObservableProperty]
    public partial BookItem? SelectedBookItem { get; set; }

    protected override string FileGlob => "*.mgxc";

    protected override bool IsFileChanged(string path)
    {
        return Model?.Books.Values.Where(p => p.Meta.FilePath == path) != null;
    }

    protected override void SetModel(OptionModel? oldModel, OptionModel? newModel)
    {
        base.SetModel(oldModel, newModel);
        SelectedBook = null;
        SelectedBookItem = null;
    }

    protected override async Task<OperationResult<OptionModel>> ReadModel(string path, OperationContext context, CancellationToken ct = default)
    {
        var diag = context.Diagnostic;
        context.ReportProgress(Strings.Status_Searching);
        var model = new OptionModel();
        await model.LoadAsync(path, ct);
        if (string.IsNullOrWhiteSpace(model.WorkingDirectory) || !Directory.Exists(model.WorkingDirectory))
        {
            model.WorkingDirectory = path;
        }

        var ctx = new ProcessContext
        {
            Context = context,
            CancellationToken = ct,
            BatchSize = model.BatchSize,
            WorkingDirectory = model.WorkingDirectory
        };

        var directoryWalker = Directory.EnumerateFiles(path, FileGlob, SearchOption.AllDirectories);
        var books = model.Books;
        await BatchAsync(Strings.Status_Checked, directoryWalker, async (filePath, innerContext) =>
        {
            ct.ThrowIfCancellationRequested();
            if (Path.GetExtension(filePath) != ".mgxc") return;
            var parser = new MgxcParser(new MgxcParseRequest(filePath, AssetManager), MediaTool, innerContext);
            var parsed = await parser.ParseAsync(ct);
            innerContext.Diagnostic.Report(parsed.Diagnostics);
            if (!parsed.Succeeded || parsed.Value is not { } chart) return;
            var meta = chart.Meta;
            var id = meta.Id ?? throw new DiagnosticException(Strings.Error_File_ignored_due_to_id_missing);
            if (!books.TryGetValue(id, out var book)) books[id] = book = new Book();
            var item = new BookItem(chart);
            if (book.Items.ContainsKey(meta.Difficulty)) innerContext.Diagnostic.Report(Severity.Warning, Strings.Warn_Duplicate_id_and_difficulty);
            book.Items[meta.Difficulty] = item;
        }, ctx);

        if (books.Count <= 0) throw new DiagnosticException(Strings.Error_No_charts_are_found_directory);
        ct.ThrowIfCancellationRequested();

        foreach (var (id, book) in books.ToArray())
        {
            ct.ThrowIfCancellationRequested();
            var items = book.Items.Values.ToArray();
            if (items.Length == 0)
            {
                books.Remove(id);
                continue;
            }

            if (book.Items.ContainsKey(Difficulty.WorldsEnd) && book.Items.Count != 1) diag.Report(Severity.Warning, Strings.Warn_We_chart_must_be_unique_id, target: items);
            var mainItems = items.Where(x => x.Mgxc.Meta.IsMain).ToArray();
            if (mainItems.Length > 1) diag.Report(Severity.Warning, Strings.Warn_More_than_one_chart_marked_main, target: mainItems);
            else if (mainItems.Length == 0 && items.Length > 1) diag.Report(Severity.Warning, Strings.Warn_No_chart_marked_main, target: mainItems);

            var mainItem = mainItems.FirstOrDefault() ?? mainItems.OrderByDescending(x => x.Difficulty).FirstOrDefault();
            if (mainItem == null)
            {
                books.Remove(id);
                continue;
            }

            book.MainDifficulty = mainItem.Difficulty;
        }

        ct.ThrowIfCancellationRequested();
        context.ReportProgress(Strings.Status_Done);

        return OperationResult<OptionModel>.Success(model);
    }

    protected override async Task<OperationResult> Action(OperationContext context, CancellationToken ct = default)
    {
        var diag = context.Diagnostic;
        var settings = Model;
        if (settings == null) return OperationResult.Success();
        if (!settings.CanExecute) throw new DiagnosticException(Strings.Error_Noop);

        var books = settings.Books;
        if (books.Count == 0) throw new DiagnosticException(Strings.Error_No_charts_are_found_directory);

        var initialDirectory = settings.WorkingDirectory;
        if (string.IsNullOrWhiteSpace(initialDirectory) || !Directory.Exists(initialDirectory))
        {
            var dir = Path.GetDirectoryName(ModelPath);
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir)) initialDirectory = dir;
        }

        var dlg = new OpenFolderDialog
        {
            ClientGuid = new Guid("C81454B6-EA09-41D6-90B2-4BD4FB3D5449"),
            Title = Strings.Title_Select_the_output_folder,
            Multiselect = false,
            ValidateNames = true,
            InitialDirectory = initialDirectory
        };
        if (dlg.ShowDialog() != true) return OperationResult.Success();
        settings.WorkingDirectory = dlg.FolderName;
        var path = settings.OptionDirectory;

        var musicFolder = Path.Combine(path, "music");
        var stageFolder = Path.Combine(path, "stage");
        var cueFileFolder = Path.Combine(path, "cueFile");
        var eventFolder = Path.Combine(path, "event");
        var releaseTagPath = Path.Combine(path, "releaseTag");

        var weEntries = new ConcurrentBag<Entry>();
        var ultEntries = new ConcurrentBag<Entry>();
        await settings.SaveAsync(ModelPath, ct);

        var ctx = new ProcessContext
        {
            Context = context,
            CancellationToken = ct,
            BatchSize = settings.BatchSize,
            WorkingDirectory = settings.WorkingDirectory
        };

        await BatchAsync(Strings.Status_Converted, settings, async (book, innerContext) =>
        {
            var stage = book.Stage;
            if (book.IsCustomStage && settings.ConvertBackground)
            {
                if (string.IsNullOrWhiteSpace(book.Meta.FullBgiFilePath)) throw new DiagnosticException(Strings.Error_Background_file_is_not_set);
                if (book.StageId is null) throw new DiagnosticException(Strings.Error_Stage_id_is_not_set);
                var stageConverter = new StageConverter(
                    new StageBuildRequest(
                        AssetManager,
                        book.Meta.FullBgiFilePath,
                        [],
                        book.StageId,
                        stageFolder,
                        book.NotesFieldLine),
                    MediaTool,
                    ResourceStore,
                    innerContext);
                var builtStage = await stageConverter.BuildAsync(ct);
                innerContext.Diagnostic.Report(builtStage.Diagnostics);
                if (!builtStage.Succeeded || builtStage.Value is not { } stageEntry) return;
                stage = stageEntry;
                ct.ThrowIfCancellationRequested();
            }

            if (settings.ConvertChart || settings.ConvertJacket)
            {
                var metaMap = book.Items.ToDictionary(x => x.Key, x => x.Value.Meta);
                var xml = new MusicXml(metaMap, book.Difficulty)
                {
                    StageName = stage
                };
                var chartFolder = await xml.SaveDirectoryAsync(musicFolder);

                if (settings.ConvertChart)
                {
                    foreach (var (diff, item) in book.Items)
                    {
                        if (item.Id is not { } songId) throw new DiagnosticException(Strings.Error_Song_id_is_not_set);
                        if (diff == Difficulty.WorldsEnd) weEntries.Add(new Entry(songId, book.Title));
                        else if (diff == Difficulty.Ultima) ultEntries.Add(new Entry(songId, book.Title));
                        var chartPath = Path.Combine(chartFolder, xml[item.Difficulty].File);
                        var chartWriter = new C2SChartWriter(new C2SWriteRequest(chartPath, item.Mgxc), innerContext);
                        var writtenChart = await chartWriter.WriteAsync(ct);
                        innerContext.Diagnostic.Report(writtenChart.Diagnostics);
                        if (!writtenChart.Succeeded) return;
                        ct.ThrowIfCancellationRequested();
                    }
                }

                if (settings.ConvertJacket)
                {
                    var jacketPath = book.Meta.FullJacketFilePath;
                    if (File.Exists(jacketPath))
                    {
                        var jacketConverter = new JacketConverter(
                            new JacketConvertRequest(jacketPath, Path.Combine(chartFolder, xml.JaketFile)),
                            MediaTool,
                            innerContext);
                        var convertedJacket = await jacketConverter.ConvertAsync(ct);
                        innerContext.Diagnostic.Report(convertedJacket.Diagnostics);
                        if (!convertedJacket.Succeeded) return;
                        ct.ThrowIfCancellationRequested();
                    }
                    else
                    {
                        innerContext.Diagnostic.Report(Severity.Warning, Strings.Error_Jacket_file_not_found, target: jacketPath);
                    }
                }
            }


            if (settings.ConvertAudio)
            {
                var musicConverter = new MusicConverter(new MusicConvertRequest(book.Meta, cueFileFolder), MediaTool, ResourceStore, innerContext);
                var convertedMusic = await musicConverter.ConvertAsync(ct);
                innerContext.Diagnostic.Report(convertedMusic.Diagnostics);
                if (!convertedMusic.Succeeded) return;
                ct.ThrowIfCancellationRequested();
            }
        }, ctx, true);

        if (settings.GenerateReleaseTagXml)
        {
            var releaseTagXml = ReleaseTag.Default;
            await releaseTagXml.SaveDirectoryAsync(releaseTagPath);
        }

        if (settings.GenerateEventXml && !ultEntries.IsEmpty)
        {
            var eventXml = new EventXml(settings.UltimaEventId, EventXml.MusicType.Ultima, ultEntries.ToHashSet());
            await eventXml.SaveDirectoryAsync(eventFolder);
            ct.ThrowIfCancellationRequested();
        }

        if (settings.GenerateEventXml && !weEntries.IsEmpty)
        {
            var eventXml = new EventXml(settings.WeEventId, EventXml.MusicType.WldEnd, weEntries.ToHashSet());
            await eventXml.SaveDirectoryAsync(eventFolder);
            ct.ThrowIfCancellationRequested();
        }

        return OperationResult.Success();
    }

    private static async Task ProcessItemsAsync<T>(string prefix, IEnumerable<T> items, Func<T, OperationContext, Task> action, Func<T, string> getPath, ProcessContext main, bool parallel = false)
    {
        var itemList = items as IList<T> ?? [..items];
        var total = itemList.Count;
        var completedCount = 0;
        main.Context.ReportProgress($"{prefix}: 0/{total}...");

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

        async ValueTask ProcessItemAsync(T item, CancellationToken ct)
        {
            var ld = new Diagnoster();
            var childContext = main.Context.CreateChild(ld);
            try
            {
                await action(item, childContext);
                ct.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                ld.Report(ex);
            }
            finally
            {
                var done = Interlocked.Increment(ref completedCount);
                main.Context.ReportProgress($"{prefix}: {done}/{total}...");
                foreach (var diagItem in ld.Diagnostics)
                {
                    diagItem.Path ??= Path.GetRelativePath(main.WorkingDirectory, getPath(item));
                    main.Context.Diagnostic.Report(diagItem);
                }
            }
        }
    }

    private static Task BatchAsync(string prefix, OptionModel model, Func<Book, OperationContext, Task> action, ProcessContext ctx, bool parallel = false)
    {
        return ProcessItemsAsync(prefix, model.Books.Values, action, b => b.Meta.FilePath, ctx, parallel);
    }

    private static Task BatchAsync(string prefix, IEnumerable<string> items, Func<string, OperationContext, Task> action, ProcessContext ctx)
    {
        return ProcessItemsAsync(prefix, items, action, item => item, ctx, true);
    }

    protected override Task Reload()
    {
        Model?.SaveAsync(ModelPath);
        return base.Reload();
    }

    private class ProcessContext
    {
        public required OperationContext Context { get; init; }
        public required CancellationToken CancellationToken { get; init; }
        public required int BatchSize { get; init; }
        public required string WorkingDirectory { get; init; }
    }
}
