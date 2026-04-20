using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Diagnostic;

namespace PenguinTools.CLI;

internal static class CliPaths
{
    internal static OperationResult Merge(DiagnosticSnapshot diagnostics, OperationResult result)
    {
        return (result.Succeeded ? OperationResult.Success() : OperationResult.Failure())
            .WithDiagnostics(diagnostics.Merge(result.Diagnostics));
    }

    internal static OperationResult<T> CreateFailureResultOf<T>(string message, string? path = null)
    {
        var sink = new DiagnosticCollector();
        sink.Report(string.IsNullOrWhiteSpace(path)
            ? new Diagnostic(Severity.Error, message)
            : new PathDiagnostic(Severity.Error, message, path));
        return OperationResult<T>.Failure().WithDiagnostics(sink);
    }

    internal static void EnsureParentDirectory(string path)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent)) Directory.CreateDirectory(parent);
    }

    internal static string ResolvePath(string path)
    {
        return Path.GetFullPath(path);
    }

    internal static string? ResolveOptionalPath(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : ResolvePath(path);
    }

    internal static Entry CreateEntry(Entry current, int? id, string? name, string? data)
    {
        if (id is null && name is null && data is null) return current;

        return new Entry(id ?? current.Id, name ?? current.Str, data ?? current.Data);
    }
}
