using PenguinTools.Common.Chart.Models;
using PenguinTools.Common.Resources;
using System.Text;

// ReSharper disable RedundantNameQualifier

namespace PenguinTools.Common.Chart;

using mgxc = Models.mgxc;
using c2s = Models.c2s;

public partial class ChartConverter : IConverter<ChartConverter.Context>
{
    private IDiagnostic diag = new DiagnosticReporter();
    private List<c2s.Note> Notes { get; set; } = [];
    private List<c2s.Event> Events { get; set; } = [];

    public async Task ConvertAsync(Context context, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        Reset();
        if (!await CanConvertAsync(context, this.diag)) return;
        progress?.Report(Strings.Status_converting_chart);
        this.diag = diag;

        var mgxc = context.Chart;
        diag.TimeCalculator = mgxc.GetCalculator();

        foreach (var note in mgxc.Notes.Children) ConvertNote(note);
        ConvertEvent(mgxc);

        progress?.Report(Strings.Status_Validate);
        ValidateChart();

        if (mgxc.Meta.BgmEnableBarOffset)
        {
            var sig = mgxc.Meta.BgmInitialTimeSignature;
            var offset = (int)Math.Round((decimal)Time.MarResolution / sig.Denominator * sig.Numerator);
            foreach (var e in Events.Where(e => e.Tick.Original != 0)) e.Tick = e.Tick.Original + offset;
            foreach (var n in Notes)
            {
                n.Tick = n.Tick.Original + offset;
                if (n is c2s.LongNote longNote) longNote.EndTick = longNote.EndTick.Original + offset;
            }
        }

        progress?.Report(Strings.Status_writing);

        var sb = new StringBuilder();
        sb.AppendLine("VERSION\t1.13.00\t1.13.00");
        sb.AppendLine("MUSIC\t0");
        sb.AppendLine("SEQUENCEID\t0");
        sb.AppendLine("DIFFICULT\t0");
        sb.AppendLine("LEVEL\t0.0");
        sb.AppendLine($"CREATOR\t{mgxc.Meta.Designer}");
        sb.AppendLine($"BPM_DEF\t{mgxc.Meta.MainBpm:F3}\t{mgxc.Meta.MainBpm:F3}\t{mgxc.Meta.MainBpm:F3}\t{mgxc.Meta.MainBpm:F3}");
        sb.AppendLine($"MET_DEF\t{mgxc.Meta.BgmInitialTimeSignature.Denominator}\t{mgxc.Meta.BgmInitialTimeSignature.Numerator}");
        sb.AppendLine("RESOLUTION\t384");
        sb.AppendLine("CLK_DEF\t384");
        sb.AppendLine("PROGJUDGE_BPM\t240.000");
        sb.AppendLine("PROGJUDGE_AER\t  0.999");
        sb.AppendLine("TUTORIAL\t0");
        sb.AppendLine();

        foreach (var e in Events)
        {
            try
            {
                sb.AppendLine(e.Text);
            }
            catch (Exception ex)
            {
                this.diag.Report(Severity.Error, ex.Message, e.Tick.Original, e);
            }
        }
        sb.AppendLine();
        foreach (var n in Notes)
        {
            try
            {
                sb.AppendLine(n.Text);
            }
            catch (Exception ex)
            {
                this.diag.Report(Severity.Error, ex.Message, n.Tick.Original, n);
            }
        }

        if (this.diag.HasErrors) return;
        await File.WriteAllTextAsync(context.OutputPath, sb.ToString(), ct);
    }

    public Task<bool> CanConvertAsync(Context context, IDiagnostic diag)
    {
        return Task.FromResult(true);
    }

    private void Reset()
    {
        pMap.Clear();
        nMap.Clear();
        Notes = [];
        Events = [];
    }

    protected void ValidateChart()
    {
        var allSlides = Notes.OfType<c2s.Slide>().ToList();
        var allAirs = Notes.OfType<c2s.IPairable>().Where(p => p.Parent is c2s.Slide).Cast<c2s.Note>().ToList();

        var airsLookup = allAirs.GroupBy(a => (a.Tick, a.Lane, a.Width)).ToDictionary(g => g.Key, g => g.Count());
        var slidesLookup = allSlides.GroupBy(s => (s.EndTick, s.EndLane, s.EndWidth)).ToDictionary(g => g.Key, g => g.Count());

        foreach (var (pos, airsCount) in airsLookup)
        {
            var slidesCount = slidesLookup.GetValueOrDefault(pos, 0);
            if (airsCount >= slidesCount) continue;
            diag.Report(Severity.Information, Strings.Overlapping_Air_Parent_Slide, pos.Tick.Original);
        }

        foreach (var longNote in Notes.OfType<c2s.LongNote>())
        {
            var length = longNote.Length.Original;
            if (length >= Time.SingleTick) continue;

            var tick = longNote.Tick.Original;
            var msg = string.Format(Strings.Diag_set_length_smaller_than_unit, length, Time.SingleTick);
            diag.Report(Severity.Warning, msg, tick, longNote);
        }
    }

    public record Context(string OutputPath, mgxc.Chart Chart);
}