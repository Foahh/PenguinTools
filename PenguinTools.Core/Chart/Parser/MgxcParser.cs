using PenguinTools.Common.Asset;
using PenguinTools.Common.Chart.Models;
using PenguinTools.Common.Metadata;
using PenguinTools.Common.Resources;
using System.Text;
using System.Text.RegularExpressions;
using mgxc = PenguinTools.Common.Chart.Models.mgxc;

namespace PenguinTools.Common.Chart.Parser;

using mgxc = mgxc;

public partial class MgxcParser(IDiagnostic diag, AssetManager asm)
{
    private const string HEADER_MGXC = "MGXC"; // 4D 47 58 43
    private const string HEADER_META = "meta"; // 6D 65 74 61
    private const string HEADER_EVNT = "evnt"; // 65 76 6E 74
    private const string HEADER_DAT2 = "dat2"; // 64 61 74 32
    private mgxc.Chart mgxc = new();
    public static string MargreteVersion => "1.8.0";

    public async Task<mgxc.Chart> ParseAsync(string path, CancellationToken ct)
    {
        mgxc = new mgxc.Chart();
        mgxc.Meta.FilePath = path;

        await using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);
        var header = br.ReadUtf8String(4);
        if (header != HEADER_MGXC) throw new DiagnosticException(string.Format(Strings.Error_Invalid_Header, header, HEADER_MGXC));
        br.ReadInt32(); // MGXC Block Size
        br.ReadInt32(); // unknown

        br.ReadBlock(HEADER_META, ParseMeta);
        ct.ThrowIfCancellationRequested();

        br.ReadBlock(HEADER_EVNT, ParseEvent);
        diag.TimeCalculator = mgxc.GetCalculator();
        ct.ThrowIfCancellationRequested();

        br.ReadBlock(HEADER_DAT2, ParseNote);
        ct.ThrowIfCancellationRequested();

        ProcessEvent();
        ct.ThrowIfCancellationRequested();

        ProcessNote();
        ct.ThrowIfCancellationRequested();

        ProcessTil();
        ct.ThrowIfCancellationRequested();

        ProcessMetaCommand();
        ProcessMeta();
        ct.ThrowIfCancellationRequested();

        return mgxc;
    }

    protected void ProcessMeta()
    {
        if (string.IsNullOrWhiteSpace(mgxc.Meta.SortName))
        {
            mgxc.Meta.SortName = GetSortName(mgxc.Meta.Title);
            diag.Report(Severity.Information, Strings.Diag_no_sortname_provided);
        }

        if (mgxc.Meta.IsCustomStage && !string.IsNullOrWhiteSpace(mgxc.Meta.FullBgiFilePath))
        {
            if (MuaInterop.IsValidImage(mgxc.Meta.FullBgiFilePath)) return;
            mgxc.Meta.IsCustomStage = false;
            diag.Report(Severity.Information, Strings.Error_invalid_bg_image, mgxc.Meta.FullBgiFilePath);
        }
    }

    protected void ProcessEvent()
    {
        var bpmEvents = mgxc.Events.Children.OfType<mgxc.BpmEvent>().OrderBy(e => e.Tick).ToList();
        if (bpmEvents.Count <= 0 || bpmEvents[0].Tick.Original != 0) throw new DiagnosticException(Strings.Error_BPM_change_event_not_found_at_0);

        var beatEvents = mgxc.Events.Children.OfType<mgxc.BeatEvent>().OrderBy(e => e.Bar).ToList();
        if (beatEvents.Count <= 0 || beatEvents[0].Bar != 0)
        {
            mgxc.Events.InsertBefore(new mgxc.BeatEvent
            {
                Bar = 0,
                Numerator = 4,
                Denominator = 4
            }, bpmEvents.FirstOrDefault());
            beatEvents = [.. mgxc.Events.Children.OfType<mgxc.BeatEvent>().OrderBy(e => e.Bar)];
            diag.Report(Severity.Information, Strings.Diag_time_Signature_event_not_found_at_0);
        }

        var initBeat = beatEvents[0];
        mgxc.Meta.BgmInitialBpm = bpmEvents[0].Bpm;
        mgxc.Meta.BgmInitialNumerator = initBeat.Numerator;
        mgxc.Meta.BgmInitialDenominator = initBeat.Denominator;

        // calculate tick for each beat event
        if (beatEvents.Count > 1)
        {
            var ticks = 0;
            for (var i = 0; i < beatEvents.Count - 1; i++)
            {
                var curr = beatEvents[i];
                var next = beatEvents[i + 1];
                ticks += Time.MarResolution * curr.Numerator / curr.Denominator * (next.Bar - curr.Bar);
                next.Tick = ticks;
            }
        }

        mgxc.Events.Sort();
    }

    protected void ProcessNote()
    {
        if (mgxc.Notes.Children.Count <= 0) return;

        var noteGroup = mgxc.Notes.Children.OfType<mgxc.ExTapableNote>().GroupBy(note => (note.Tick, note.Lane, note.Width)).ToDictionary(g => g.Key, g => g.ToList());
        var exEffects = new Dictionary<Time, HashSet<ExEffect>>();
        var remove = new List<mgxc.ExTap>();

        foreach (var exTap in mgxc.Notes.Children.OfType<mgxc.ExTap>())
        {
            if (!exEffects.TryGetValue(exTap.Tick, out var effectSet))
            {
                effectSet = [];
                exEffects[exTap.Tick] = effectSet;
            }
            effectSet.Add(exTap.Effect);

            var key = (exTap.Tick, exTap.Lane, exTap.Width);
            if (!noteGroup.TryGetValue(key, out var matchingNotes)) continue;
            foreach (var note in matchingNotes) note.Effect = exTap.Effect;
            if (exTap.Children.Count <= 0 && exTap.PairNote == null) remove.Add(exTap);
        }
        foreach (var exTap in remove) mgxc.Notes.RemoveChild(exTap);

        mgxc.Notes.Sort();

        foreach (var (tick, effects) in exEffects)
        {
            if (effects.Count <= 1) continue;
            var str = string.Join(", ", effects.Select(e => e.ToString()));
            var msg = string.Format(Strings.Diag_concurrent_ex_effects, str);
            diag.Report(Severity.Information, msg, tick.Original);
        }
    }

    public static string GetSortName(string? s)
    {
        if (s is null) return string.Empty;
        var t = s.ToUpperInvariant().Normalize(NormalizationForm.FormKC);
        t = WhitespaceRegex().Replace(t, "_");
        t = SpecialCharacterRegex().Replace(t, "");
        return t;
    }

    [GeneratedRegex(@"\s+")] private static partial Regex WhitespaceRegex();
    [GeneratedRegex(@"[^\p{L}\p{N}_]")] private static partial Regex SpecialCharacterRegex();
}