using PenguinTools.Core;
using PenguinTools.Media;

namespace PenguinTools.CLI;

internal static class CliDiagnostics
{
    internal static void WriteDiagnostics(DiagnosticSnapshot snapshot)
    {
        foreach (var diagnostic in snapshot.Diagnostics.OrderByDescending(d => d.Severity)
                     .ThenBy(d => d.Path, StringComparer.Ordinal)
                     .ThenBy(d => d.Time)
                     .ThenBy(d => d.Message, StringComparer.Ordinal))
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
        if (!string.IsNullOrWhiteSpace(diagnostic.Path))
        {
            details.Add(diagnostic.Path);
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
        if (exception is DiagnosticException diagnosticException)
        {
            var sink = new Diagnoster();
            sink.Report(new Diagnostic(Severity.Error, diagnosticException.Message, diagnosticException.Path, diagnosticException.Tick, diagnosticException.Target));
            WriteDiagnostics(DiagnosticSnapshot.Create(sink));
            return;
        }

        Console.Error.WriteLine($"error: {exception.Message}");
    }
}
