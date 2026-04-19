using PenguinTools.Chart.Models;
using PenguinTools.Chart.Resources;
using PenguinTools.Core;

namespace PenguinTools.Chart.Parser;

using mg = Models.mgxc;

public partial class UgcParser
{
    private void DispatchBodyLine(string line)
    {
        // Parent: #<Bar>'<Tick>:<payload>[,<suffix>]
        // Child:  #<offsetTick>><payload>
        if (!line.StartsWith('#')) return;
        var rest = line.AsSpan(1);

        var childIdx = rest.IndexOf('>');
        var parentIdx = rest.IndexOf(':');

        if (childIdx >= 0 && (parentIdx < 0 || childIdx < parentIdx))
        {
            if (!int.TryParse(rest[..childIdx], out var offset))
            {
                WarnMalformed(line);
                return;
            }

            var payload = rest[(childIdx + 1)..].ToString();
            HandleChildPayload(offset, payload);
            return;
        }

        if (parentIdx < 0)
        {
            WarnMalformed(line);
            return;
        }

        var bt = rest[..parentIdx];
        var tickSepIdx = bt.IndexOf('\'');
        if (tickSepIdx <= 0)
        {
            WarnMalformed(line);
            return;
        }

        if (!int.TryParse(bt[..tickSepIdx], out var bar) || !int.TryParse(bt[(tickSepIdx + 1)..], out var tick))
        {
            WarnMalformed(line);
            return;
        }

        var rhs = rest[(parentIdx + 1)..].ToString();
        var commaIdx = rhs.IndexOf(',');
        var payloadStr = commaIdx >= 0 ? rhs[..commaIdx] : rhs;
        var suffixStr = commaIdx >= 0 ? rhs[(commaIdx + 1)..] : string.Empty;

        var absTick = BarTickToAbsTick(bar, tick);
        HandleParentPayload(absTick, payloadStr, suffixStr);
    }

    private void HandleParentPayload(int absTick, string payload, string suffix)
    {
        if (payload.Length < 3)
        {
            WarnMalformed(payload);
            return;
        }

        var typeChar = payload[0];
        var x = UgcPayload.Base36(payload[1]);
        var w = UgcPayload.Base36(payload[2]);
        if (x < 0 || w < 0)
        {
            WarnMalformed(payload);
            return;
        }

        var extras = payload.Length > 3 ? payload[3..] : string.Empty;

        mg.Note? note = typeChar switch
        {
            't' => new mg.Tap(),
            'x' => MakeExTap(extras),
            'f' => new mg.Flick(),
            'd' => new mg.Damage(),
            'c' => null,
            _ => HandleLongNoteParent(typeChar, extras, suffix)
        };

        if (note is null)
        {
            if (typeChar != 'c') WarnUnknownType(typeChar);
            return;
        }

        note.Tick = absTick;
        note.Lane = x;
        note.Width = w;
        note.Timeline = _currentTimeline;

        Ugc.Notes.AppendChild(note);
        _lastParentNote = note;
        _lastNote = note;
    }

    private static mg.ExTap MakeExTap(string extras)
    {
        var exNote = new mg.ExTap();
        exNote.Effect = extras.Length >= 1 ? UgcPayload.ExEffectChar(extras[0]) : ExEffect.UP;
        return exNote;
    }

    private mg.Note? HandleLongNoteParent(char typeChar, string extras, string suffix) =>
        typeChar switch
        {
            'h' => new mg.Hold(),
            's' => new mg.Slide(),
            _ => null
        };

    private void HandleChildPayload(int offsetTick, string payload)
    {
        if (payload.Length < 3)
        {
            WarnMalformed(payload);
            return;
        }

        if (_lastParentNote is null)
        {
            WarnMalformed(payload);
            return;
        }

        var typeChar = payload[0];
        var x = UgcPayload.Base36(payload[1]);
        var w = UgcPayload.Base36(payload[2]);
        if (x < 0 || w < 0)
        {
            WarnMalformed(payload);
            return;
        }

        var absTick = _lastParentNote.Tick.Original + offsetTick;

        mg.Note? child = null;
        switch (typeChar)
        {
            case 's' when _lastParentNote is mg.Hold:
                child = new mg.HoldJoint();
                break;
            case 's' when _lastParentNote is mg.Slide:
            {
                var jointChar = payload.Length >= 4 ? payload[3] : 'D';
                var jointKind = jointChar switch
                {
                    'C' => Joint.C,
                    'D' => Joint.D,
                    'E' => Joint.D,
                    _ => Joint.D
                };
                child = new mg.SlideJoint { Joint = jointKind };
                break;
            }
        }

        if (child is null) return;

        child.Tick = absTick;
        child.Lane = x;
        child.Width = w;
        child.Timeline = _lastParentNote.Timeline;
        _lastParentNote.AppendChild(child);
        _lastNote = child;
    }

    private void WarnMalformed(string what) =>
        Diagnostic.Report(Severity.Warning, string.Format(Strings.Mg_Unrecognized_note, what, 0));

    private void WarnUnknownType(char c) =>
        Diagnostic.Report(Severity.Warning, string.Format(Strings.Mg_Unrecognized_note, c.ToString(), 0));
}
