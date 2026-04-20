namespace PenguinTools.Core.Diagnostic;

public interface IDiagnosticSink
{
    IReadOnlyCollection<Diagnostic> Diagnostics { get; }
    bool HasProblem { get; }
    bool HasError { get; }
    ITickFormatter? TimeCalculator { get; set; }

    void Report(Diagnostic item);
    void Report(Exception ex);
}