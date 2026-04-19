using PenguinTools.Core;
using PenguinTools.Core.Asset;

namespace PenguinTools.Workflow;

internal static class MusicPaths
{
    internal static OperationResult<T> CreateFailureResultOf<T>(string message, string? path = null)
    {
        var sink = new Diagnoster();
        sink.Report(Severity.Error, message, path);
        return OperationResult<T>.Failure().WithDiagnostics(DiagnosticSnapshot.Create(sink));
    }

    internal static void EnsureParentDirectory(string path)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }
    }

    internal static Entry CreateEntry(Entry current, int? id, string? name, string? data)
    {
        if (id is null && name is null && data is null)
        {
            return current;
        }

        return new Entry(id ?? current.Id, name ?? current.Str, data ?? current.Data);
    }
}
