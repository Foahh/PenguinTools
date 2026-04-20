namespace PenguinTools.Core.Diagnostic;

public class LocationDiagnosticException(string message, int line, string? path = null, object? target = null)
    : DiagnosticException(message, target)
{
    public string? Path { get; } = path;
    public int Line { get; } = line;

    public override Diagnostic ToDiagnostic()
    {
        return new LocationDiagnostic(Severity.Error, Message, Line, Path)
        {
            Target = Target,
            RelatedException = this,
            TimeCalculator = TimeCalculator
        };
    }
}