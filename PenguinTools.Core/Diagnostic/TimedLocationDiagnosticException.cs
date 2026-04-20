namespace PenguinTools.Core;

public class TimedLocationDiagnosticException(
    string message,
    int line,
    int tick,
    string? path = null,
    object? target = null) : DiagnosticException(message, target)
{
    public string? Path { get; } = path;
    public int Line { get; } = line;
    public int Tick { get; } = tick;

    public override Diagnostic ToDiagnostic()
    {
        return new TimedLocationDiagnostic(Severity.Error, Message, Line, Tick, Path)
        {
            Target = Target,
            RelatedException = this,
            TimeCalculator = TimeCalculator
        };
    }
}