using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.i18n;
using PenguinTools.Media;
using PenguinTools.Models;
using PenguinTools.Workflow;

namespace PenguinTools.Services;

public sealed class ChartScanService : IChartScanService
{
    private readonly AssetManager _assetManager;
    private readonly ActionService _actionService;
    private readonly IMediaTool _mediaTool;

    public ChartScanService(AssetManager assetManager, IMediaTool mediaTool, ActionService actionService)
    {
        _assetManager = assetManager;
        _mediaTool = mediaTool;
        _actionService = actionService;
    }

    public async Task<OperationResult> ScanAsync(string directory, BookDictionary books, ChartScanParameters parameters,
        CancellationToken ct)
    {
        var scanResult = await ChartScanner.ScanDirectoryAsync(
            _assetManager,
            _mediaTool,
            directory,
            parameters.ChartFileDiscovery,
            parameters.BatchSize,
            parameters.WorkingDirectory,
            parameters.Diagnostics,
            ct,
            CreateMessages(),
            _actionService);

        if (!scanResult.Succeeded || scanResult.Value is not { } snapshots)
            return scanResult.ToResult();

        ApplySnapshots(books, snapshots, ct);
        return scanResult.ToResult();
    }

    private static void ApplySnapshots(
        BookDictionary books,
        IEnumerable<OptionBookSnapshot> snapshots,
        CancellationToken ct)
    {
        foreach (var snapshot in snapshots)
        {
            ct.ThrowIfCancellationRequested();
            var id = snapshot.BookMeta.Id;
            if (id is null) continue;

            Book book;
            lock (books)
            {
                if (!books.TryGetValue(id.Value, out book!))
                {
                    book = new Book();
                    books[id.Value] = book;
                }
            }

            lock (book)
            {
                foreach (var item in snapshot.Difficulties.Values)
                    book.Items[item.Difficulty] = new BookItem(item.Chart);

                book.MainDifficulty = snapshot.BookMeta.Difficulty;
            }
        }
    }

    private static ChartScannerMessages CreateMessages()
    {
        return new ChartScannerMessages(
            Strings.Status_Checked,
            Strings.Error_File_ignored_due_to_id_missing,
            Strings.Warn_Duplicate_id_and_difficulty,
            Strings.Warn_We_chart_must_be_unique_id,
            Strings.Warn_More_than_one_chart_marked_main,
            Strings.Warn_No_chart_marked_main);
    }
}
