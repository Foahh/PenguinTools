namespace PenguinTools.Core;

public static class DiagnosticSinkExtensions
{
    public static void Report(this IDiagnosticSink sink, DiagnosticSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(snapshot);

        foreach (var diagnostic in snapshot.Diagnostics) sink.Report(diagnostic.Copy());
    }
}