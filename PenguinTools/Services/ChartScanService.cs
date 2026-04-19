using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Metadata;
using PenguinTools.Chart.Parser;
using PenguinTools.Media;
using PenguinTools.Resources;
using PenguinTools.Models;
using PenguinTools.Workflow;
using System.IO;

namespace PenguinTools.Services;

using mg = PenguinTools.Chart.Models.mgxc;

public sealed class ChartScanService : IChartScanService
{
    private const string MgxcExtension = ".mgxc";
    private const string UgcExtension = ".ugc";

    private readonly AssetManager _assetManager;
    private readonly IMediaTool _mediaTool;

    public ChartScanService(AssetManager assetManager, IMediaTool mediaTool)
    {
        _assetManager = assetManager;
        _mediaTool = mediaTool;
    }

    public async Task<OperationResult> ScanAsync(string directory, BookDictionary books, ChartScanParameters parameters, CancellationToken ct)
    {
        var processContext = new OptionExportProcessContext(parameters.Diagnostics, ct, parameters.BatchSize, parameters.WorkingDirectory);
        DiagnosticSnapshot batch = parameters.ChartFileDiscovery switch
        {
            ChartFileDiscoveryMode.MgxcOnly =>
                await LoadBooksFromGlobAsync(directory, "*.mgxc", books, processContext, skipIfDifficultyFilled: false, ct),
            ChartFileDiscoveryMode.UgcOnly =>
                await LoadBooksFromGlobAsync(directory, "*.ugc", books, processContext, skipIfDifficultyFilled: false, ct),
            ChartFileDiscoveryMode.MgxcFirst =>
                (await LoadBooksFromGlobAsync(directory, "*.mgxc", books, processContext, skipIfDifficultyFilled: false, ct))
                .Merge(await LoadBooksFromGlobAsync(directory, "*.ugc", books, processContext, skipIfDifficultyFilled: true, ct)),
            ChartFileDiscoveryMode.UgcFirst =>
                (await LoadBooksFromGlobAsync(directory, "*.ugc", books, processContext, skipIfDifficultyFilled: false, ct))
                .Merge(await LoadBooksFromGlobAsync(directory, "*.mgxc", books, processContext, skipIfDifficultyFilled: true, ct)),
            _ => DiagnosticSnapshot.Empty
        };

        FinalizeBooks(books, parameters.Diagnostics, ct);
        return OperationResult.Success().WithDiagnostics(batch.Merge(DiagnosticSnapshot.Create(parameters.Diagnostics)));
    }

    private async Task<DiagnosticSnapshot> LoadBooksFromGlobAsync(
        string path,
        string fileGlob,
        BookDictionary books,
        OptionExportProcessContext context,
        bool skipIfDifficultyFilled,
        CancellationToken ct)
    {
        var chartPaths = Directory.EnumerateFiles(path, fileGlob, SearchOption.AllDirectories);
        return await OptionExportBatch.BatchAsync(
            Strings.Status_Checked,
            chartPaths,
            (filePath, innerDiagnostics) => LoadBookAsync(filePath, books, innerDiagnostics, skipIfDifficultyFilled, ct),
            filePath => filePath,
            context,
            parallel: true);
    }

    private async Task LoadBookAsync(string filePath, BookDictionary books, IDiagnosticSink diagnostics, bool skipIfDifficultyFilled, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ext = Path.GetExtension(filePath);
        mg.Chart? chart = null;
        if (string.Equals(ext, UgcExtension, StringComparison.OrdinalIgnoreCase))
        {
            var r = await new UgcParser(new UgcParseRequest(filePath, _assetManager), _mediaTool).ParseAsync(ct);
            diagnostics.Report(r.Diagnostics);
            if (!r.Succeeded || r.Value is not { } ugcChart) return;
            chart = ugcChart;
        }
        else if (string.Equals(ext, MgxcExtension, StringComparison.OrdinalIgnoreCase))
        {
            var r = await new MgxcParser(new MgxcParseRequest(filePath, _assetManager), _mediaTool).ParseAsync(ct);
            diagnostics.Report(r.Diagnostics);
            if (!r.Succeeded || r.Value is not { } mgxcChart) return;
            chart = mgxcChart;
        }
        else return;

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
            if (skipIfDifficultyFilled && book.Items.ContainsKey(meta.Difficulty)) return;

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
}
