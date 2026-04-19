using System.Text;
using PenguinTools.Chart.Models;
using PenguinTools.Chart.Resources;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Media;

namespace PenguinTools.Chart.Parser.ugc;

using umgr = Models.umgr;

public partial class UgcParser
{
    private readonly record struct SourceLine(int Number, string Text);

    static UgcParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public UgcParser(UgcParseRequest request, IMediaTool mediaTool)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mediaTool);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Path);
        ArgumentNullException.ThrowIfNull(request.Assets);

        MediaTool = mediaTool;
        Path = request.Path;
        Assets = request.Assets;
    }

    private IMediaTool MediaTool { get; }
    private IDiagnosticSink Diagnostic { get; } = new Diagnoster();
    private string Path { get; }
    private AssetManager Assets { get; }
    private List<Task> Tasks { get; } = [];
    private umgr.Chart Ugc { get; } = new();

    private int _currentTimeline;
    private umgr.Note? _lastNote;
    private umgr.Note? _lastParentNote;
    private int? _currentLineNumber;

    public async Task<OperationResult<umgr.Chart>> ParseAsync(CancellationToken ct = default)
    {
        try
        {
            Ugc.Meta.FilePath = Path;
            var lines = await ReadLinesAsync(Path, ct);

            foreach (var line in lines)
            {
                ct.ThrowIfCancellationRequested();
                SetCurrentLine(line);
                if (line.Text.StartsWith('@')) DispatchHeaderLine(line.Text);
            }

            ClearCurrentLine();
            BuildBarAxis();

            _currentTimeline = 0;
            foreach (var line in lines)
            {
                ct.ThrowIfCancellationRequested();
                SetCurrentLine(line);
                if (line.Text.StartsWith("@USETIL", StringComparison.Ordinal)) ApplyUseTil(line.Text);
                else if (line.Text.StartsWith('#')) DispatchBodyLine(line.Text);
            }

            ClearCurrentLine();
            Diagnostic.TimeCalculator = Ugc.GetCalculator();

            var post = new ChartPostProcessor(Ugc, Diagnostic, Assets);
            post.Run();
            ProcessMeta();

            await Task.WhenAll(Tasks);
            return OperationResult<umgr.Chart>.Success(Ugc).WithDiagnostics(DiagnosticSnapshot.Create(Diagnostic));
        }
        catch (DiagnosticException ex)
        {
            Diagnostic.Report(ex);
            return OperationResult<umgr.Chart>.Failure().WithDiagnostics(DiagnosticSnapshot.Create(Diagnostic));
        }
    }

    private static async Task<SourceLine[]> ReadLinesAsync(string path, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(path, ct);
        string text;
        try
        {
            text = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            text = Encoding.GetEncoding(932).GetString(bytes);
        }

        using var reader = new StringReader(text);
        var lines = new List<SourceLine>();
        for (var lineNumber = 1; ; lineNumber++)
        {
            var line = reader.ReadLine();
            if (line is null) break;
            lines.Add(new SourceLine(lineNumber, line));
        }

        return [.. lines];
    }

    private void SetCurrentLine(SourceLine line)
    {
        _currentLineNumber = line.Number;
    }

    private void ClearCurrentLine()
    {
        _currentLineNumber = null;
    }

    private void ReportAtCurrentLine(Severity severity, string message, object? target = null)
    {
        Diagnostic.Report(new Diagnostic(severity, message, Path, target: target, line: _currentLineNumber));
    }

    private void ReportAtCurrentLine(Severity severity, string message, int tick, object? target = null)
    {
        Diagnostic.Report(new Diagnostic(severity, message, Path, tick, target, _currentLineNumber));
    }

    private void ThrowAtCurrentLine(string message, object? target = null, int? tick = null)
    {
        throw new DiagnosticException(message, target, tick, Path, _currentLineNumber);
    }

    private void BuildBarAxis()
    {
        var beats = Ugc.Events.Children.OfType<umgr.BeatEvent>().OrderBy(b => b.Bar).ToList();
        if (beats.Count > 0)
        {
            beats[0].Tick = 0;
            var accum = 0;
            for (var i = 0; i < beats.Count - 1; i++)
            {
                var curr = beats[i];
                var next = beats[i + 1];
                accum += ChartResolution.UmiguriTick * curr.Numerator / curr.Denominator * (next.Bar - curr.Bar);
                next.Tick = accum;
            }
        }

        foreach (var (bar, tick, bpm) in _pendingBpms)
            Ugc.Events.AppendChild(new umgr.BpmEvent { Tick = BarTickToAbsTick(bar, tick), Bpm = bpm });

        foreach (var (bar, tick, spd) in _pendingSpdMods)
            Ugc.Events.AppendChild(new umgr.NoteSpeedEvent { Tick = BarTickToAbsTick(bar, tick), Speed = spd });

        foreach (var (tilId, bar, tick, spd) in _pendingTils)
            Ugc.Events.AppendChild(new umgr.ScrollSpeedEvent { Timeline = tilId, Tick = BarTickToAbsTick(bar, tick), Speed = spd });
    }

    private void ProcessMeta()
    {
        if (string.IsNullOrWhiteSpace(Ugc.Meta.SortName))
        {
            Ugc.Meta.SortName = ChartPostProcessor.GetSortName(Ugc.Meta.Title);
            Diagnostic.Report(Severity.Information, Strings.Mg_No_sortname_provided);
        }

        if (Ugc.Meta.IsCustomStage && !string.IsNullOrWhiteSpace(Ugc.Meta.FullBgiFilePath))
        {
            QueueValidation(
                MediaTool.CheckImageValidAsync(Ugc.Meta.FullBgiFilePath),
                Ugc.Meta.FullBgiFilePath,
                Strings.Error_Invalid_bg_image,
                () =>
                {
                    Ugc.Meta.IsCustomStage = false;
                    Ugc.Meta.BgiFilePath = string.Empty;
                });
        }
    }

    private void QueueValidation(Task<ProcessCommandResult> validationTask, string path, string message, Action onFailure)
    {
        Tasks.Add(HandleValidationAsync(validationTask, path, message, onFailure));
    }

    private async Task HandleValidationAsync(Task<ProcessCommandResult> validationTask, string path, string message, Action onFailure)
    {
        try
        {
            var result = await validationTask;
            if (result.IsSuccess) return;

            onFailure();
            Diagnostic.Report(Severity.Warning, message, path, result);
        }
        catch (Exception ex)
        {
            onFailure();
            Diagnostic.Report(Severity.Warning, message, path, ex);
        }
    }
}
