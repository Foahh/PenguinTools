using PenguinTools.Core.Chart.Models;
using PenguinTools.Core.Resources;
using System.Text;

// ReSharper disable RedundantNameQualifier

namespace PenguinTools.Core.Chart.Writer;

using mg = Models.mgxc;
using c2s = Models.c2s;

public partial class C2SChartWriter
{
    public C2SChartWriter(C2SWriteRequest request, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutPath);
        ArgumentNullException.ThrowIfNull(request.Mgxc);

        ParentContext = context;
        CurrentContext = context;
        OutPath = request.OutPath;
        Mgxc = request.Mgxc;
    }

    private OperationContext ParentContext { get; }
    private OperationContext CurrentContext { get; set; }
    private IDiagnosticSink Diagnostic => CurrentContext.Diagnostic;
    private IProgress<string>? Progress => CurrentContext.Progress;
    private string OutPath { get; }
    private mg.Chart Mgxc { get; }
    private List<c2s.Note> Notes { get; } = [];
    private List<c2s.Event> Events { get; } = [];

    public async Task<OperationResult> WriteAsync(CancellationToken ct = default)
    {
        var diagnostics = new Diagnoster
        {
            TimeCalculator = ParentContext.Diagnostic.TimeCalculator
        };
        CurrentContext = ParentContext.CreateChild(diagnostics);

        try
        {
            Progress?.Report(Strings.Status_Converting_chart);

            Diagnostic.TimeCalculator = Mgxc.GetCalculator();

            foreach (var note in Mgxc.Notes.Children) ConvertNote(note);
            ResolvePairings();
            ConvertEvent(Mgxc);

            // Post Validation
            Progress?.Report(Strings.Status_Validate);
            var allSlides = Notes.OfType<c2s.Slide>();
            var allAirs = Notes.OfType<c2s.IPairable>().Where(p => p.Parent is c2s.Slide).Cast<c2s.Note>();

            var airsLookup = allAirs.GroupBy(a => (a.Tick, a.Lane, a.Width)).ToDictionary(g => g.Key, g => g.Count());
            var slidesLookup = allSlides.GroupBy(s => (s.EndTick, s.EndLane, s.EndWidth)).ToDictionary(g => g.Key, g => g.Count());

            foreach (var (pos, airsCount) in airsLookup)
            {
                var slidesCount = slidesLookup.GetValueOrDefault(pos, 0);
                if (airsCount >= slidesCount) continue;
                Diagnostic.Report(Severity.Warning, Strings.Mg_Overlapping_air_parent_slide, pos.Tick.Original);
            }

            foreach (var longNote in Notes.OfType<c2s.LongNote>())
            {
                var length = longNote.Length.Original;
                if (length >= ChartResolution.SingleTick) continue;

                var tick = longNote.Tick.Original;
                var msg = string.Format(Strings.Mg_Length_smaller_than_unit, length, ChartResolution.MarResolution / ChartResolution.SingleTick);
                Diagnostic.Report(Severity.Warning, msg, tick, longNote);
            }

            if (Mgxc.Meta.BgmEnableBarOffset)
            {
                var offset = (int)Math.Round((decimal)ChartResolution.MarResolution / Mgxc.Meta.BgmInitialDenominator * Mgxc.Meta.BgmInitialNumerator);
                foreach (var e in Events.Where(e => e.Tick.Original != 0)) e.Tick = e.Tick.Original + offset;
                foreach (var n in Notes)
                {
                    n.Tick = n.Tick.Original + offset;
                    if (n is c2s.LongNote longNote) longNote.EndTick = longNote.EndTick.Original + offset;
                }
            }

            Progress?.Report(Strings.Status_Writing);

            var sb = new StringBuilder();
            sb.AppendLine("VERSION\t1.13.00\t1.13.00");
            sb.AppendLine("MUSIC\t0");
            sb.AppendLine("SEQUENCEID\t0");
            sb.AppendLine("DIFFICULT\t0");
            sb.AppendLine("LEVEL\t0.0");
            sb.AppendLine($"CREATOR\t{Mgxc.Meta.Designer}");
            sb.AppendLine($"BPM_DEF\t{Mgxc.Meta.MainBpm:F3}\t{Mgxc.Meta.MainBpm:F3}\t{Mgxc.Meta.MainBpm:F3}\t{Mgxc.Meta.MainBpm:F3}");
            sb.AppendLine($"MET_DEF\t{Mgxc.Meta.BgmInitialDenominator}\t{Mgxc.Meta.BgmInitialNumerator}");
            sb.AppendLine("RESOLUTION\t384");
            sb.AppendLine("CLK_DEF\t384");
            sb.AppendLine("PROGJUDGE_BPM\t240.000");
            sb.AppendLine("PROGJUDGE_AER\t  0.999");
            sb.AppendLine("TUTORIAL\t0");
            sb.AppendLine();

            AppendFormattedEvents(sb);
            sb.AppendLine();
            if (!AppendFormattedNotes(sb))
            {
                return OperationResult.Failure().WithDiagnostics(DiagnosticSnapshot.Create(diagnostics));
            }

            await File.WriteAllTextAsync(OutPath, sb.ToString(), ct);
            return OperationResult.Success().WithDiagnostics(DiagnosticSnapshot.Create(diagnostics));
        }
        finally
        {
            CurrentContext = ParentContext;
        }
    }

    private void AppendFormattedEvents(StringBuilder sb)
    {
        foreach (var e in Events)
        {
            sb.AppendLine(Format(e));
        }
    }

    private bool AppendFormattedNotes(StringBuilder sb)
    {
        var hasError = false;
        foreach (var n in Notes)
        {
            if (TryFormat(n, out var line, out var error))
            {
                sb.AppendLine(line);
                continue;
            }

            Diagnostic.Report(Severity.Error, error, n.Tick.Original, n);
            hasError = true;
        }

        return !hasError;
    }
}
