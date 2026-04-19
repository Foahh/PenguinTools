using System.Collections.Concurrent;
using System.IO;
using PenguinTools.Chart.Parser;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Metadata;
using PenguinTools.Media;

namespace PenguinTools.Workflow;

public static class OptionChartScanner
{
    private const string MgxcExtension = ".mgxc";

    private sealed class BookAccumulator
    {
        public readonly object Gate = new();
        public readonly Dictionary<Difficulty, OptionDifficultySnapshot> Items = new();
    }

    public static async Task<OperationResult<IReadOnlyList<OptionBookSnapshot>>> ScanDirectoryAsync(
        AssetManager assets,
        IMediaTool mediaTool,
        string directory,
        string fileGlob,
        int batchSize,
        string workingDirectory,
        IDiagnosticSink diagnostics,
        CancellationToken ct)
    {
        var chartPaths = Directory.EnumerateFiles(directory, fileGlob, SearchOption.AllDirectories);
        var booksById = new ConcurrentDictionary<int, BookAccumulator>();
        var processContext = new OptionExportProcessContext(diagnostics, ct, batchSize, workingDirectory);

        var batchDiagnostics = await OptionExportBatch.BatchAsync(
            "scan",
            chartPaths,
            (filePath, innerDiagnostics) => LoadChartAsync(filePath, assets, mediaTool, booksById, innerDiagnostics, ct),
            filePath => filePath,
            processContext,
            parallel: true);

        var snapshots = FinalizeAndBuildSnapshots(booksById, diagnostics, ct);
        return OperationResult<IReadOnlyList<OptionBookSnapshot>>.Success(snapshots)
            .WithDiagnostics(batchDiagnostics.Merge(DiagnosticSnapshot.Create(diagnostics)));
    }

    private static async Task LoadChartAsync(
        string filePath,
        AssetManager assets,
        IMediaTool mediaTool,
        ConcurrentDictionary<int, BookAccumulator> booksById,
        IDiagnosticSink diagnostics,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!string.Equals(Path.GetExtension(filePath), MgxcExtension, StringComparison.OrdinalIgnoreCase)) return;

        var parser = new MgxcParser(new MgxcParseRequest(filePath, assets), mediaTool);
        var parsed = await parser.ParseAsync(ct);
        diagnostics.Report(parsed.Diagnostics);
        if (!parsed.Succeeded || parsed.Value is not { } chart) return;

        var meta = chart.Meta;
        var id = meta.Id ?? throw new DiagnosticException("File ignored because song id is missing.");
        var item = new OptionDifficultySnapshot(meta.Difficulty, meta.Id, chart, meta);
        var book = booksById.GetOrAdd(id, _ => new BookAccumulator());

        lock (book.Gate)
        {
            if (book.Items.ContainsKey(meta.Difficulty))
            {
                diagnostics.Report(Severity.Warning, "Duplicate song id and difficulty.", target: filePath);
            }

            book.Items[meta.Difficulty] = item;
        }
    }

    private static IReadOnlyList<OptionBookSnapshot> FinalizeAndBuildSnapshots(
        ConcurrentDictionary<int, BookAccumulator> booksById,
        IDiagnosticSink diagnostics,
        CancellationToken ct)
    {
        var list = new List<OptionBookSnapshot>();

        foreach (var (id, book) in booksById.ToArray())
        {
            ct.ThrowIfCancellationRequested();
            OptionDifficultySnapshot[] items;
            lock (book.Gate)
            {
                items = book.Items.Values.ToArray();
            }

            if (items.Length == 0)
            {
                booksById.TryRemove(id, out _);
                continue;
            }

            lock (book.Gate)
            {
                if (book.Items.ContainsKey(Difficulty.WorldsEnd) && book.Items.Count != 1)
                {
                    diagnostics.Report(Severity.Warning, "World's End chart must be the only difficulty for its song id.", target: items);
                }
            }

            var mainItems = items.Where(i => i.Meta.IsMain).ToArray();
            if (mainItems.Length > 1)
            {
                diagnostics.Report(Severity.Warning, "More than one chart is marked as main.", target: mainItems);
            }
            else if (mainItems.Length == 0 && items.Length > 1)
            {
                diagnostics.Report(Severity.Warning, "No chart is marked as main.", target: items);
            }

            var mainItem = mainItems.FirstOrDefault() ?? items.OrderByDescending(i => i.Difficulty).FirstOrDefault();
            if (mainItem is null)
            {
                booksById.TryRemove(id, out _);
                continue;
            }

            IReadOnlyDictionary<Difficulty, OptionDifficultySnapshot> dict;
            lock (book.Gate)
            {
                dict = book.Items.ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            list.Add(new OptionBookSnapshot(
                mainItem.Meta,
                mainItem.Meta.IsCustomStage,
                mainItem.Meta.StageId,
                mainItem.Meta.NotesFieldLine,
                mainItem.Meta.Stage,
                mainItem.Meta.Title,
                dict));
        }

        return list;
    }
}
