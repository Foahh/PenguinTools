namespace PenguinTools.Core;

public class TimedDiagnosticException(string message, int tick, object? target = null)
    : DiagnosticException(message, target)
{
    public int Tick { get; } = tick;

    public override Diagnostic ToDiagnostic()
    {
        return new TimedDiagnostic(Severity.Error, Message, Tick)
        {
            Target = Target,
            RelatedException = this,
            TimeCalculator = TimeCalculator
        };
    }
}