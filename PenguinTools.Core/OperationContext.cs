namespace PenguinTools.Core;

public sealed class OperationContext
{
    public OperationContext(IDiagnosticSink diagnostic, IProgress<string>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        Diagnostic = diagnostic;
        Progress = progress;
    }

    public IDiagnosticSink Diagnostic { get; }
    public IProgress<string>? Progress { get; }

    public void ReportProgress(string status)
    {
        Progress?.Report(status);
    }

    public OperationContext CreateChild(IDiagnosticSink diagnostic, IProgress<string>? progress = null)
    {
        return new OperationContext(diagnostic, progress ?? Progress);
    }
}
