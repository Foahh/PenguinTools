using PenguinTools.Core.Asset;
using PenguinTools.Core.Chart.Models;
using PenguinTools.Core.Media;
using PenguinTools.Core.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace PenguinTools.Core.Chart.Parser;

using mg = Models.mgxc;

public partial class MgxcParser(IDiagnostic diag, IProgress<string>? prog = null) : ConverterBase<mg.Chart>(diag, prog)
{
    private const string HEADER_MGXC = "MGXC"; // 4D 47 58 43
    private const string HEADER_META = "meta"; // 6D 65 74 61
    private const string HEADER_EVNT = "evnt"; // 65 76 6E 74
    private const string HEADER_DAT2 = "dat2"; // 64 61 74 32

    public required string Path { get; init; }
    public required AssetManager Assets { get; init; }

    private List<Task> Tasks { get; } = [];
    private mg.Chart Mgxc { get; } = new();

    protected async override Task<mg.Chart> ActionAsync(CancellationToken ct = default)
    {
        Mgxc.Meta.FilePath = Path;

        await using var fs = File.OpenRead(Path);
        using var br = new BinaryReader(fs);

        var header = br.ReadUtf8String(4);
        if (header != HEADER_MGXC) throw new DiagnosticException(string.Format(Strings.Error_Invalid_Header, header, HEADER_MGXC));

        br.ReadInt32(); // MGXC Block Size
        br.ReadInt32(); // unknown

        br.ReadBlock(HEADER_META, ParseMeta);

        br.ReadBlock(HEADER_EVNT, ParseEvent);

        Diagnostic.TimeCalculator = Mgxc.GetCalculator();

        br.ReadBlock(HEADER_DAT2, ParseNote);

        ProcessEvent();

        ProcessNote();

        ProcessTil();

        ProcessCommand();
        ProcessMeta();

        await Task.WhenAll(Tasks);
        return Mgxc;
    }

    private void ProcessMeta()
    {
        if (string.IsNullOrWhiteSpace(Mgxc.Meta.SortName))
        {
            Mgxc.Meta.SortName = GetSortName(Mgxc.Meta.Title);
            Diagnostic.Report(Severity.Information, Strings.Mg_No_sortname_provided);
        }

        if (Mgxc.Meta.IsCustomStage && !string.IsNullOrWhiteSpace(Mgxc.Meta.FullBgiFilePath))
        {
            Tasks.Add(Manipulate.IsImageValidAsync(Mgxc.Meta.FullBgiFilePath).ContinueWith(p =>
            {
                if (p.Result.IsSuccess) return;
                Mgxc.Meta.IsCustomStage = false;
                Diagnostic.Report(Severity.Warning, Strings.Error_Invalid_bg_image, Mgxc.Meta.FullBgiFilePath, target: p.Result);
                Mgxc.Meta.BgiFilePath = string.Empty;
            }));
        }
    }

    private void ProcessEvent()
    {
        var bpmEvents = Mgxc.Events.Children.OfType<mg.BpmEvent>().OrderBy(e => e.Tick).ToList();
        if (bpmEvents.Count <= 0 || bpmEvents[0].Tick.Original != 0) throw new DiagnosticException(Strings.Mg_Head_BPM_not_found);

        var beatEvents = Mgxc.Events.Children.OfType<mg.BeatEvent>().OrderBy(e => e.Bar).ToList();
        if (beatEvents.Count <= 0 || beatEvents[0].Bar != 0)
        {
            Mgxc.Events.InsertBefore(new mg.BeatEvent
            {
                Bar = 0,
                Numerator = 4,
                Denominator = 4
            }, bpmEvents.FirstOrDefault());
            beatEvents = [..Mgxc.Events.Children.OfType<mg.BeatEvent>().OrderBy(e => e.Bar)];
            Diagnostic.Report(Severity.Information, Strings.Mg_Head_Time_Signature_event_not_found);
        }

        var initBeat = beatEvents[0];
        Mgxc.Meta.BgmInitialBpm = bpmEvents[0].Bpm;
        Mgxc.Meta.BgmInitialNumerator = initBeat.Numerator;
        Mgxc.Meta.BgmInitialDenominator = initBeat.Denominator;

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

        Mgxc.Events.Sort();
    }

    private void ProcessNote()
    {
        if (Mgxc.Notes.Children.Count <= 0) return;

        var noteGroup = Mgxc.Notes.Children.OfType<mg.ExTapableNote>().GroupBy(note => (note.Tick, note.Lane, note.Width)).ToDictionary(g => g.Key, g => g.ToList());
        var exEffects = new Dictionary<Time, HashSet<ExEffect>>();
        var remove = new List<mg.ExTap>();

        foreach (var exTap in Mgxc.Notes.Children.OfType<mg.ExTap>())
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
        foreach (var exTap in remove) Mgxc.Notes.RemoveChild(exTap);

        Mgxc.Notes.Sort();

        foreach (var (tick, effects) in exEffects)
        {
            if (effects.Count <= 1) continue;
            var str = string.Join(", ", effects.Select(e => e.ToString()));
            var msg = string.Format(Strings.Mg_Concurrent_ex_effects, str);
            Diagnostic.Report(Severity.Information, msg, tick.Original);
        }
    }

    private static string GetSortName(string? s)
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