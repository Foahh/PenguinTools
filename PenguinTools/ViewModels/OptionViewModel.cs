using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using PenguinTools.Common;
using PenguinTools.Common.Asset;
using PenguinTools.Common.Audio;
using PenguinTools.Common.Chart;
using PenguinTools.Common.Chart.Parser;
using PenguinTools.Common.Graphic;
using PenguinTools.Common.Metadata;
using PenguinTools.Common.Resources;
using PenguinTools.Common.Xml;
using PenguinTools.Models;
using System.Collections.Concurrent;
using System.IO;
using System.Media;

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

    protected async override Task<OptionModel> ReadModel(string path, IDiagnostic d, IProgress<string> p, CancellationToken ct = default)
    {
        p.Report(Strings.Status_Searching);
        var model = new OptionModel();
        await model.LoadAsync(path, ct);

        var ctx = new ProcessContext
        {
            Diagnostic = d,
            Progress = p,
            CancellationToken = ct,
            BatchSize = model.BatchSize,
            WorkingDirectory = model.WorkingDirectory
        };

        var walker = Directory.EnumerateFiles(path, FileGlob, SearchOption.AllDirectories);
        var books = model.Books;
        await BatchAsync(Strings.Status_Checked, walker, async (file, d) =>
        {
            ct.ThrowIfCancellationRequested();
            if (Path.GetExtension(file) != ".mgxc") return;
            var parser = new MgxcParser(d, AssetManager);
            var chart = await parser.ParseAsync(file, ct);
            var meta = chart.Meta;
            var id = meta.Id ?? throw new DiagnosticException(Strings.Diag_file_ignored_due_to_id_missing);
            if (!books.TryGetValue(id, out var book)) books[id] = book = new Book();
            var item = new BookItem(chart);
            if (book.Items.ContainsKey(meta.Difficulty)) d.Report(Severity.Warning, Strings.Diag_duplicate_id_and_difficulty, target: file);
            book.Items[meta.Difficulty] = item;
        }, ctx);

        if (books.Count <= 0) throw new DiagnosticException(Strings.Error_no_charts_are_found_directory);
        ct.ThrowIfCancellationRequested();

        foreach (var (id, book) in books.ToList())
        {
            ct.ThrowIfCancellationRequested();
            var items = book.Items.Values.ToArray();
            if (items.Length == 0)
            {
                books.Remove(id);
                continue;
            }

            if (book.Items.ContainsKey(Difficulty.WorldsEnd) && book.Items.Count != 1) d.Report(Severity.Error, Strings.Diag_we_chart_must_be_unique_id, target: items);
            var mainItems = items.Where(x => x.Chart.Meta.IsMain).ToArray();
            if (mainItems.Length > 1) d.Report(Severity.Warning, Strings.Diag_more_than_one_chart_marked_main, target: mainItems);
            else if (mainItems.Length == 0 && items.Length > 1) d.Report(Severity.Warning, Strings.Diag_no_chart_marked_main, target: mainItems);

            var mainItem = mainItems.FirstOrDefault() ?? mainItems.OrderByDescending(x => x.Difficulty).FirstOrDefault();
            if (mainItem == null)
            {
                books.Remove(id);
                continue;
            }

            book.MainDifficulty = mainItem.Difficulty;
        }

        ct.ThrowIfCancellationRequested();
        p.Report(Strings.Status_done);

        return model;
    }

    protected async override Task Action()
    {
        var settings = Model;
        if (settings == null) return;
        if (!settings.CanExecute) throw new DiagnosticException(Strings.Error_but_nobody_came);

        var books = settings.Books;
        if (books.Count == 0) throw new DiagnosticException(Strings.Error_no_charts_are_found_directory);

        var initialDirectory = settings.WorkingDirectory;
        if (string.IsNullOrWhiteSpace(initialDirectory) || !Directory.Exists(initialDirectory))
        {
            var dir = Path.GetDirectoryName(ModelPath);
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir)) initialDirectory = dir;
        }

        var dlg = new OpenFolderDialog
        {
            ClientGuid = new Guid("C81454B6-EA09-41D6-90B2-4BD4FB3D5449"),
            Title = Strings.Title_select_the_output_folder,
            Multiselect = false,
            ValidateNames = true,
            InitialDirectory = initialDirectory
        };
        if (dlg.ShowDialog() != true) return;
        settings.WorkingDirectory = dlg.FolderName;

        var path = Path.Combine(settings.WorkingDirectory, settings.OptionName);
        var musicFolder = Path.Combine(path, "music");
        var stageFolder = Path.Combine(path, "stage");
        var cueFileFolder = Path.Combine(path, "cueFile");
        var eventFolder = Path.Combine(path, "event");
        var releaseTagPath = Path.Combine(path, "releaseTag");

        await ActionService.RunAsync(async (d, p, ct) =>
        {
            var weEntries = new ConcurrentBag<Entry>();
            var ultEntries = new ConcurrentBag<Entry>();
            await settings.SaveAsync(ModelPath, ct);

            var ctx = new ProcessContext
            {
                Diagnostic = d,
                Progress = p,
                CancellationToken = ct,
                BatchSize = settings.BatchSize,
                WorkingDirectory = settings.WorkingDirectory
            };

            var stageConverter = new StageConverter(AssetManager);
            var jacketConverter = new JacketConverter();
            var musicConverter = new MusicConverter();

            await BatchAsync(Strings.Status_Converted, settings, async (book, d) =>
            {
                var stage = book.Stage;
                if (book.IsCustomStage && settings.ConvertBackground)
                {
                    if (string.IsNullOrWhiteSpace(book.Meta.FullBgiFilePath)) throw new DiagnosticException(Strings.Error_background_file_is_not_set);
                    if (book.StageId is null) throw new DiagnosticException(Strings.Error_stage_id_is_not_set);
                    var stageOpts = new StageConverter.Context(book.Meta.FullBgiFilePath, [], book.StageId, stageFolder, book.NotesFieldLine);
                    await stageConverter.ConvertAsync(stageOpts, d, null, ct);
                    if (d.HasError) throw new DiagnosticException(Strings.Error_Stage_Failed);
                    stage = stageOpts.Result;
                    ct.ThrowIfCancellationRequested();
                }

                if (settings.ConvertChart || settings.ConvertJacket)
                {
                    var metaMap = book.Items.ToDictionary(x => x.Key, x => x.Value.Meta);
                    var xml = new MusicXml(metaMap, book.Difficulty) { StageName = stage };
                    var chartFolder = await xml.SaveDirectoryAsync(musicFolder);

                    if (settings.ConvertChart)
                    {
                        var chartConverter = new ChartConverter();
                        foreach (var (diff, item) in book.Items)
                        {
                            if (item.Id is not { } songId) throw new DiagnosticException(Strings.Error_song_id_is_not_set);
                            if (diff == Difficulty.WorldsEnd) weEntries.Add(new Entry(songId, book.Title));
                            else if (diff == Difficulty.Ultima) ultEntries.Add(new Entry(songId, book.Title));
                            var chartPath = Path.Combine(chartFolder, xml[item.Difficulty].File);
                            var chartOpts = new ChartConverter.Context(chartPath, item.Chart);
                            await chartConverter.ConvertAsync(chartOpts, d, null, ct);
                            if (d.HasError) throw new DiagnosticException(Strings.Error_Chart_Failed);
                            ct.ThrowIfCancellationRequested();
                        }
                    }

                    if (settings.ConvertJacket)
                    {
                        var jacketOpts = new JacketConverter.Context(book.Meta.FullJacketFilePath, Path.Combine(chartFolder, xml.JaketFile));
                        await jacketConverter.ConvertAsync(jacketOpts, d, null, ct);
                        if (d.HasError) throw new DiagnosticException(Strings.Error_Jacket_Failed);
                        ct.ThrowIfCancellationRequested();
                    }
                }


                if (settings.ConvertAudio)
                {
                    var musicOpts = new MusicConverter.Context(book.Meta, cueFileFolder);
                    await musicConverter.ConvertAsync(musicOpts, d, null, ct);
                    if (d.HasError) throw new DiagnosticException(Strings.Error_Music_Failed);
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
        });

        SystemSounds.Exclamation.Play();
    }

    private async static Task ProcessItemsAsync<T>(string prefix, IEnumerable<T> items, Func<T, IDiagnostic, Task> action, Func<T, string> getPath, ProcessContext ctx, bool parallel = false)
    {
        var itemList = items as IList<T> ?? [.. items];
        var total = itemList.Count;
        var completedCount = 0;
        ctx.Progress.Report($"{prefix}: 0/{total}...");

        if (parallel)
        {
            await Parallel.ForEachAsync(itemList, new ParallelOptions { CancellationToken = ctx.CancellationToken, MaxDegreeOfParallelism = ctx.BatchSize }, ProcessItemAsync);
        }
        else
        {
            foreach (var item in itemList) await ProcessItemAsync(item, ctx.CancellationToken);
        }

        async ValueTask ProcessItemAsync(T item, CancellationToken ct)
        {
            var ld = new DiagnosticReporter();
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
                ctx.Progress.Report($"{prefix}: {done}/{total}...");
                foreach (var diagItem in ld.Diagnostics)
                {
                    diagItem.Path ??= Path.GetRelativePath(ctx.WorkingDirectory, getPath(item));
                    ctx.Diagnostic.Report(diagItem);
                }
            }
        }
    }

    private static Task BatchAsync(string prefix, OptionModel model, Func<Book, IDiagnostic, Task> action, ProcessContext ctx, bool parallel = false)
    {
        return ProcessItemsAsync(prefix, model.Books.Values, action, b => b.Meta.FilePath, ctx, parallel);
    }

    private static Task BatchAsync(string prefix, IEnumerable<string> items, Func<string, IDiagnostic, Task> action, ProcessContext ctx)
    {
        return ProcessItemsAsync(prefix, items, action, item => item, ctx, true);
    }

    protected override Task Reload()
    {
        Model?.SaveAsync(ModelPath);
        return base.Reload();
    }

    private sealed class ProcessContext
    {
        public required IDiagnostic Diagnostic { get; init; }
        public required IProgress<string> Progress { get; init; }
        public required CancellationToken CancellationToken { get; init; }
        public required int BatchSize { get; init; }
        public required string WorkingDirectory { get; init; }
    }
}