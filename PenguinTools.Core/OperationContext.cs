namespace PenguinTools.Core;

public sealed class OperationContext
{
    public OperationContext(IDiagnosticSink diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        Diagnostic = diagnostic;
    }

    public IDiagnosticSink Diagnostic { get; }

    public OperationContext CreateChild(IDiagnosticSink diagnostic)
    {
        return new OperationContext(diagnostic);
    }
}
