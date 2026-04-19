using System.Collections.Concurrent;
using PenguinTools.Core.Resources;

namespace PenguinTools.Core;

public enum Severity
{
    Information = 1,
    Warning = 2,
    Error = 3
}

public class Diagnostic(
    Severity severity,
    string message,
    string? path = null,
    int? time = null,
    object? target = null,
    int? line = null) : IComparable<Diagnostic>, IComparable
{
    public Severity Severity { get; set; } = severity;
    public string Message { get; set; } = message;
    public string? Path { get; set; } = path;
    public int? Time { get; set; } = time;
    public object? Target { get; set; } = target;
    public int? Line { get; set; } = line;

    public Exception? RelatedException { get; set; }

    public ITickFormatter? TimeCalculator { get; set; }

    public string? FormattedLocation
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Path)) return Line is null ? null : $"0x{Line.Value:X2}";

            if (Line is null) return Path;

            return string.Equals(System.IO.Path.GetExtension(Path), ".mgxc", StringComparison.OrdinalIgnoreCase)
                ? $"{Path}(0x{Line.Value:X2})"
                : $"{Path}({Line.Value})";
        }
    }

    public string? FormattedTime
    {
        get
        {
            if (Time is null) return null;
            if (TimeCalculator is null) return string.Format(Strings.Unit_Tick, Time);
            return TimeCalculator.FormatTick(Time.Value);
        }
    }

    public Diagnostic Copy()
    {
        return new Diagnostic(Severity, Message, Path, Time, Target, Line)
        {
            RelatedException = RelatedException,
            TimeCalculator = TimeCalculator
        };
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
        var lineComparison = Nullable.Compare(Line, other.Line);
        if (lineComparison != 0) return lineComparison;
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

public sealed class DiagnosticSnapshot
{
    private DiagnosticSnapshot(IReadOnlyList<Diagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
    }

    public static DiagnosticSnapshot Empty { get; } = new([]);

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public bool HasProblem => Diagnostics.Count > 0;
    public bool HasError => Diagnostics.Any(d => d.Severity == Severity.Error);

    public DiagnosticSnapshot Merge(DiagnosticSnapshot other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (!HasProblem) return other;
        if (!other.HasProblem) return this;
        return Create(Diagnostics.Concat(other.Diagnostics));
    }

    public static DiagnosticSnapshot Create(IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new DiagnosticSnapshot([.. diagnostics.Select(d => d.Copy())]);
    }

    public static DiagnosticSnapshot Create(IDiagnosticSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        return Create(sink.Diagnostics);
    }
}

public interface IDiagnosticSink
{
    IReadOnlyCollection<Diagnostic> Diagnostics { get; }
    bool HasProblem { get; }
    bool HasError { get; }
    ITickFormatter? TimeCalculator { get; set; }

    void Report(Diagnostic item);
    void Report(Exception ex);
    void Report(Severity severity, string message, string? path = null, object? target = null);
    void Report(Severity severity, string message, int tick, object? target = null);
}

public class Diagnoster : IDiagnosticSink
{
    private readonly ConcurrentBag<Diagnostic> _diags = [];
    public IReadOnlyCollection<Diagnostic> Diagnostics => _diags;

    public bool HasProblem => !_diags.IsEmpty;
    public bool HasError => _diags.Any(d => d.Severity == Severity.Error);

    public ITickFormatter? TimeCalculator { get; set; }

    public void Report(Diagnostic item)
    {
        if (item.TimeCalculator == null) item.TimeCalculator = TimeCalculator;
        _diags.Add(item);
    }

    public void Report(Exception ex)
    {
        if (ex is DiagnosticException dEx)
        {
            _diags.Add(new Diagnostic(Severity.Error, ex.Message, dEx.Path, dEx.Tick, dEx.Target, dEx.Line)
                { RelatedException = dEx });
            return;
        }

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

public class DiagnosticException(
    string message,
    object? target = null,
    int? tick = null,
    string? path = null,
    int? line = null) : Exception(message)
{
    public object? Target { get; } = target;
    public string? Path { get; } = path;
    public int? Tick { get; } = tick;
    public int? Line { get; } = line;

    public ITickFormatter? TimeCalculator { get; set; } = null;
}

public static class DiagnosticSinkExtensions
{
    public static void Report(this IDiagnosticSink sink, DiagnosticSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(snapshot);

        foreach (var diagnostic in snapshot.Diagnostics) sink.Report(diagnostic.Copy());
    }
}

public interface ITickFormatter
{
    string FormatTick(int tick);
}