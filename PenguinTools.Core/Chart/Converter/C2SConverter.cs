using PenguinTools.Core.Chart.Models;
using PenguinTools.Core.Resources;
using System.Text;

// ReSharper disable RedundantNameQualifier

namespace PenguinTools.Core.Chart.Converter;

using mg = Models.mgxc;
using c2s = Models.c2s;

public partial class C2SConverter(Diagnoster diag, IProgress<string>? prog = null) : ConverterBase(diag, prog)
{
    public required string OutPath { get; init; }
    public required mg.Chart Mgxc { get; init; }

    private List<c2s.Note> Notes { get; set; } = [];
    private List<c2s.Event> Events { get; set; } = [];

    protected override async Task ActionAsync(CancellationToken ct = default)
    {
        Progress?.Report(Strings.Status_Converting_chart);

        Diagnostic.TimeCalculator = Mgxc.GetCalculator();

        foreach (var note in Mgxc.Notes.Children) ConvertNote(note);
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
            Diagnostic.Report(Severity.Information, Strings.Mg_Overlapping_air_parent_slide, pos.Tick.Original);
        }

        foreach (var longNote in Notes.OfType<c2s.LongNote>())
        {
            var length = longNote.Length.Original;
            if (length >= Time.SingleTick) continue;

            var tick = longNote.Tick.Original;
            var msg = string.Format(Strings.Mg_Length_smaller_than_unit, length, Time.MarResolution / Time.SingleTick);
            Diagnostic.Report(Severity.Warning, msg, tick, longNote);
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

        foreach (var e in Events)
        {
            try
            {
                sb.AppendLine(e.Text);
            }
            catch (Exception ex)
            {
                Diagnostic.Report(Severity.Error, ex.Message, e.Tick.Original, e);
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
                Diagnostic.Report(Severity.Error, ex.Message, n.Tick.Original, n);
            }
        }

        if (Diagnostic.HasError) return;
        await File.WriteAllTextAsync(OutPath, sb.ToString(), ct);
    }
}