using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Diagnostic;

public record Diagnostic(Severity Severity, string Message) : IComparable<Diagnostic>, IComparable
{
    public object? Target { get; init; }
    public Exception? RelatedException { get; init; }
    public ITickFormatter? TimeCalculator { get; init; }

    public virtual string? Path => null;
    public virtual int? Line => null;
    public virtual int? Time => null;
    public virtual string? FormattedLocation => null;

    public string? FormattedTime
    {
        get
        {
            if (Time is not { } tick) return null;
            return TimeCalculator is null
                ? string.Format(Strings.Unit_Tick, tick)
                : TimeCalculator.FormatTick(tick);
        }
    }

    public virtual Diagnostic WithPathFallback(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return new PathDiagnostic(Severity, Message, path)
        {
            Target = Target,
            RelatedException = RelatedException,
            TimeCalculator = TimeCalculator
        };
    }

    public Diagnostic WithTimeCalculator(ITickFormatter? timeCalculator)
    {
        if (TimeCalculator is not null || timeCalculator is null) return this;

        return this with { TimeCalculator = timeCalculator };
    }

    public virtual Diagnostic Copy()
    {
        return this with { };
    }

    public int CompareTo(Diagnostic? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;

        var severityComparison = Severity.CompareTo(other.Severity);
        if (severityComparison != 0) return severityComparison;

        var pathComparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
        if (pathComparison != 0) return pathComparison;

        var lineComparison = Nullable.Compare(Line, other.Line);
        if (lineComparison != 0) return lineComparison;

        var timeComparison = Nullable.Compare(Time, other.Time);
        if (timeComparison != 0) return timeComparison;

        return string.Compare(Message, other.Message, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj)
    {
        if (obj is Diagnostic other) return CompareTo(other);
        return obj is null ? 1 : 0;
    }
}