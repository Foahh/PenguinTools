using PenguinTools.Core;
using PenguinTools.Media;

namespace PenguinTools.CLI;

internal static class CliDiagnostics
{
    internal static CliDiagnosticPayload[] ToPayload(DiagnosticSnapshot snapshot)
    {
        return [.. GetOrderedDiagnostics(snapshot).Select(ToPayload)];
    }

    internal static void WriteDiagnostics(DiagnosticSnapshot snapshot)
    {
        foreach (var diagnostic in GetOrderedDiagnostics(snapshot))
        {
            var writer = diagnostic.Severity == Severity.Information ? Console.Out : Console.Error;
            writer.WriteLine(FormatDiagnostic(diagnostic));

            if (diagnostic.Target is ProcessCommandResult commandResult)
            {
                writer.WriteLine($"  command: {commandResult.Command}");

                if (!string.IsNullOrWhiteSpace(commandResult.StandardOutput))
                {
                    writer.WriteLine($"  stdout: {commandResult.StandardOutput}");
                }

                if (!string.IsNullOrWhiteSpace(commandResult.StandardError))
                {
                    writer.WriteLine($"  stderr: {commandResult.StandardError}");
                }
            }
        }
    }

    internal static DiagnosticSnapshot SnapshotFromMessage(string message)
    {
        var sink = new Diagnoster();
        sink.Report(Severity.Error, message);
        return DiagnosticSnapshot.Create(sink);
    }

    internal static DiagnosticSnapshot SnapshotFromException(Exception exception)
    {
        var sink = new Diagnoster();

        if (exception is DiagnosticException diagnosticException)
        {
            sink.Report(new Diagnostic(
                Severity.Error,
                diagnosticException.Message,
                diagnosticException.Path,
                diagnosticException.Tick,
                diagnosticException.Target,
                diagnosticException.Line));

            return DiagnosticSnapshot.Create(sink);
        }

        sink.Report(Severity.Error, exception.Message);
        return DiagnosticSnapshot.Create(sink);
    }

    internal static string FormatDiagnostic(Diagnostic diagnostic)
    {
        var severity = diagnostic.Severity switch
        {
            Severity.Information => "info",
            Severity.Warning => "warning",
            Severity.Error => "error",
            _ => "diagnostic"
        };

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(diagnostic.FormattedLocation))
        {
            details.Add(diagnostic.FormattedLocation);
        }

        if (!string.IsNullOrWhiteSpace(diagnostic.FormattedTime))
        {
            details.Add(diagnostic.FormattedTime);
        }

        return details.Count == 0
            ? $"{severity}: {diagnostic.Message}"
            : $"{severity}: {diagnostic.Message} ({string.Join(", ", details)})";
    }

    internal static void WriteException(Exception exception)
    {
        WriteDiagnostics(SnapshotFromException(exception));
    }

    private static IEnumerable<Diagnostic> GetOrderedDiagnostics(DiagnosticSnapshot snapshot)
    {
        return snapshot.Diagnostics.OrderByDescending(d => d.Severity)
            .ThenBy(d => d.Path, StringComparer.Ordinal)
            .ThenBy(d => d.Line)
            .ThenBy(d => d.Time)
            .ThenBy(d => d.Message, StringComparer.Ordinal);
    }

    private static CliDiagnosticPayload ToPayload(Diagnostic diagnostic)
    {
        return new CliDiagnosticPayload(
            ToSeverity(diagnostic.Severity),
            diagnostic.Message,
            diagnostic.Path,
            diagnostic.Line,
            diagnostic.Time,
            diagnostic.FormattedTime,
            diagnostic.Target is ProcessCommandResult commandResult ? ToPayload(commandResult) : null);
    }

    private static CliProcessPayload ToPayload(ProcessCommandResult result)
    {
        return new CliProcessPayload(
            result.Command,
            (int)result.ExitCode,
            result.ExitCode.ToString(),
            string.IsNullOrWhiteSpace(result.StandardOutput) ? null : result.StandardOutput,
            string.IsNullOrWhiteSpace(result.StandardError) ? null : result.StandardError);
    }

    private static string ToSeverity(Severity severity)
    {
        return severity switch
        {
            Severity.Information => "info",
            Severity.Warning => "warning",
            Severity.Error => "error",
            _ => "diagnostic"
        };
    }
}
