namespace PenguinTools.Core;

public class DiagnosticException(string message, object? target = null) : Exception(message)
{
    public object? Target { get; } = target;
    public ITickFormatter? TimeCalculator { get; init; }

    public virtual Diagnostic ToDiagnostic()
    {
        return new Diagnostic(Severity.Error, Message)
        {
            Target = Target,
            RelatedException = this,
            TimeCalculator = TimeCalculator
        };
    }
}