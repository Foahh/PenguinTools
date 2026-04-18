namespace PenguinTools.Core;

public sealed class OperationContext
{
    public OperationContext(Diagnoster diagnostic, IProgress<string>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        Diagnostic = diagnostic;
        Progress = progress;
    }

    public Diagnoster Diagnostic { get; }
    public IProgress<string>? Progress { get; }

    public bool HasError => Diagnostic.HasError;
    public bool HasProblem => Diagnostic.HasProblem;

    public void ReportProgress(string status)
    {
        Progress?.Report(status);
    }

    public OperationContext CreateChild(Diagnoster diagnostic, IProgress<string>? progress = null)
    {
        return new OperationContext(diagnostic, progress ?? Progress);
    }
}
