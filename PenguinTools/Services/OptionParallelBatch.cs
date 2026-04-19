using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using PenguinTools.Core;
using PenguinTools.Core.Resources;

namespace PenguinTools.Services;

internal sealed record OptionProcessContext(
    IDiagnosticSink Diagnostics,
    CancellationToken CancellationToken,
    int BatchSize,
    string WorkingDirectory);

internal static class OptionParallelBatch
{
    internal static Diagnoster CreateDiagnoster(IDiagnosticSink? parent = null) =>
        new()
        {
            TimeCalculator = parent?.TimeCalculator
        };

    internal static async Task<DiagnosticSnapshot> ProcessItemsAsync<T>(
        string prefix,
        IEnumerable<T> items,
        Func<T, IDiagnosticSink, Task> action,
        Func<T, string> getPath,
        OptionProcessContext main,
        bool parallel = false)
    {
        var itemList = items as IList<T> ?? [.. items];
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
                Interlocked.Increment(ref completedCount);
                diagnostics.Add(CreateItemDiagnostics(ld, getPath(item), main.WorkingDirectory));
            }
        }
    }

    internal static DiagnosticSnapshot CreateItemDiagnostics(IDiagnosticSink sink, string path, string workingDirectory)
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

    internal static Task<DiagnosticSnapshot> BatchAsync<T>(
        string prefix,
        IEnumerable<T> items,
        Func<T, IDiagnosticSink, Task> action,
        Func<T, string> getPath,
        OptionProcessContext context,
        bool parallel = false) =>
        ProcessItemsAsync(prefix, items, action, getPath, context, parallel);
}
