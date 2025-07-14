using PenguinTools.Core.Chart;
using System.Collections.Concurrent;

namespace PenguinTools.Core;

public enum Severity
{
    Information = 1,
    Warning = 2,
    Error = 3
}

public class Diagnostic(Severity severity, string message, string? path = null, int? time = null, object? target = null) : IComparable<Diagnostic>, IComparable
{
    public Severity Severity { get; set; } = severity;
    public string Message { get; set; } = message;
    public string? Path { get; set; } = path;
    public int? Time { get; set; } = time;
    public object? Target { get; set; } = target;

    public Exception? RelatedException { get; set; }

    public TimeCalculator? TimeCalculator { get; set; }
    public string? FormattedTime
    {
        get
        {
            if (TimeCalculator is null || Time is null) return Time?.ToString();
            var pos = TimeCalculator.GetPositionFromTick(Time.Value);
            return pos.ToString();
        }
    }

    #region IComparable

    public int CompareTo(Diagnostic? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        var severityComparison = Severity.CompareTo(other.Severity);
        if (severityComparison != 0) return severityComparison;
        var pathComparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
        if (pathComparison != 0) return pathComparison;
        var timeComparison = Nullable.Compare(Time, other.Time);
        if (timeComparison != 0) return timeComparison;
        return string.Compare(Message, other.Message, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj)
    {
        if (obj is Diagnostic other) return CompareTo(other);
        if (obj is null) return 1;
        return 0;
    }

    #endregion
}

public interface IDiagnostic
{
    bool HasProblem { get; }
    bool HasError { get; }
    TimeCalculator? TimeCalculator { get; set; }

    void Report(Diagnostic item);
    void Report(Exception ex);
    void Report(Severity severity, string message, string? path = null, object? target = null);
    void Report(Severity severity, string message, int tick, object? target = null);
}

public class DiagnosticReporter : IDiagnostic
{
    private readonly ConcurrentBag<Diagnostic> diags = [];
    public IReadOnlyCollection<Diagnostic> Diagnostics => diags;

    public bool HasProblem => !diags.IsEmpty;
    public bool HasError => diags.Any(d => d.Severity == Severity.Error);

    public TimeCalculator? TimeCalculator { get; set; }

    public void Report(Diagnostic item)
    {
        item.TimeCalculator = TimeCalculator;
        diags.Add(item);
    }

    public void Report(Exception ex)
    {
        if (ex is DiagnosticException dEx) diags.Add(new Diagnostic(Severity.Error, ex.Message, dEx.Path, dEx.Tick, dEx.Target) { RelatedException = dEx });
        Report(new Diagnostic(Severity.Error, ex.Message) { RelatedException = ex });
    }

    public void Report(Severity severity, string message, string? path = null, object? target = null)
    {
        Report(new Diagnostic(severity, message, path, null, target));
    }

    public void Report(Severity severity, string message, int tick, object? target = null)
    {
        Report(new Diagnostic(severity, message, null, tick, target));
    }
}

public class DiagnosticException(string message, object? target = null, int? tick = null, string? path = null) : Exception(message)
{
    public object? Target { get; } = target;
    public string? Path { get; } = path;
    public int? Tick { get; } = tick;

    public TimeCalculator? TimeCalculator { get; set; } = null;
}