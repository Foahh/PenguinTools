using PenguinTools.Chart.Models;
using PenguinTools.Chart.Resources;
using PenguinTools.Core;

namespace PenguinTools.Chart.Parser;

using umgr = Models.umgr;

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

        if (typeChar == 'a')
        {
            var air = new umgr.Air();
            var dirChar = extras.Length >= 1 ? extras[0] : 'U';
            var colorChar = extras.Length >= 2 ? extras[1] : 'N';
            air.Direction = UgcPayload.AirDirectionChar(dirChar);
            air.Color = UgcPayload.AirColorChar(colorChar);

            air.Timeline = _currentTimeline;
            Ugc.Notes.AppendChild(air);

            var pairPositive = FindPairPositive(absTick);
            if (pairPositive != null)
                pairPositive.MakePair(air);
            else
                ReportAtCurrentLine(Severity.Warning, Strings.MgCrit_Pairing_notes_incompatible, absTick);

            _lastNote = air;
            return;
        }

        if (typeChar is 'H' or 'S')
        {
            var heightChar = extras.Length >= 1 ? extras[0] : '0';
            var heightRaw = UgcPayload.Base36(heightChar);
            if (heightRaw < 0) heightRaw = 0;
            var height = heightRaw * 2;

            var airSlide = new umgr.AirSlide { Height = height, Color = Color.DEF };
            airSlide.Timeline = _currentTimeline;
            Ugc.Notes.AppendChild(airSlide);

            if (_lastNote is umgr.Air oldAir && oldAir.Tick.Original == absTick)
            {
                airSlide.Color = oldAir.Color;
                oldAir.Parent?.RemoveChild(oldAir);
                _lastNote = oldAir.PairNote;
            }

            var pairPositive = FindPairPositive(absTick);
            if (pairPositive != null)
                pairPositive.MakePair(airSlide);

            _lastParentNote = airSlide;
            _lastNote = airSlide;
            return;
        }

        if (typeChar == 'C')
        {
            var colorChar = extras.Length >= 1 ? extras[0] : '0';
            var heightChar = extras.Length >= 2 ? extras[1] : '0';
            var heightRaw = UgcPayload.Base36(heightChar);
            if (heightRaw < 0) heightRaw = 0;
            var height = heightRaw * 2;
            var density = 0;
            if (!string.IsNullOrEmpty(suffix) && int.TryParse(suffix, out var parsedDensity))
                density = parsedDensity;

            var crash = new umgr.AirCrash
            {
                Color = UgcPayload.CrushColorChar(colorChar),
                Height = height,
                Density = density
            };
            crash.Tick = absTick;
            crash.Lane = x;
            crash.Width = w;
            crash.Timeline = _currentTimeline;
            Ugc.Notes.AppendChild(crash);
            _lastParentNote = crash;
            _lastNote = crash;
            return;
        }

        umgr.Note? note = typeChar switch
        {
            't' => new umgr.Tap(),
            'x' => MakeExTap(extras),
            'f' => new umgr.Flick(),
            'd' => new umgr.Damage(),
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

    // Last PositiveNote at absTick when _lastNote is a non-positive long parent (Hold/Slide).
    private umgr.PositiveNote? FindPairPositive(int absTick)
    {
        if (_lastNote is umgr.PositiveNote lastP && lastP.Tick.Original == absTick)
            return lastP;

        return Ugc.Notes.Children.OfType<umgr.PositiveNote>().LastOrDefault(p => p.Tick.Original == absTick);
    }

    private static umgr.ExTap MakeExTap(string extras)
    {
        var exNote = new umgr.ExTap();
        exNote.Effect = extras.Length >= 1 ? UgcPayload.ExEffectChar(extras[0]) : ExEffect.UP;
        return exNote;
    }

    private umgr.Note? HandleLongNoteParent(char typeChar, string extras, string suffix) =>
        typeChar switch
        {
            'h' => new umgr.Hold(),
            's' => new umgr.Slide(),
            _ => null
        };

    private void HandleChildPayload(int offsetTick, string payload)
    {
        if (_lastParentNote is null)
        {
            WarnMalformed(payload);
            return;
        }

        if (payload.Trim() == "s" && _lastParentNote is umgr.Hold hold)
        {
            var hj = new umgr.HoldJoint
            {
                Tick = hold.Tick.Original + offsetTick,
                Lane = hold.Lane,
                Width = hold.Width,
                Timeline = hold.Timeline
            };
            hold.AppendChild(hj);
            _lastNote = hj;
            return;
        }

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

        var absTick = _lastParentNote.Tick.Original + offsetTick;

        umgr.Note? child = null;
        switch (typeChar)
        {
            case 's' when _lastParentNote is umgr.Hold:
                child = new umgr.HoldJoint();
                break;
            case 's' when _lastParentNote is umgr.Slide:
            {
                var jointChar = payload.Length >= 4 ? payload[3] : 'D';
                var jointKind = jointChar switch
                {
                    'C' => Joint.C,
                    'D' => Joint.D,
                    'E' => Joint.D,
                    _ => Joint.D
                };
                child = new umgr.SlideJoint { Joint = jointKind };
                break;
            }
            case 'H':
            case 'S':
                if (_lastParentNote is umgr.AirSlide)
                {
                    var hChar = payload.Length >= 4 ? payload[3] : '0';
                    var hRaw = UgcPayload.Base36(hChar);
                    if (hRaw < 0) hRaw = 0;
                    child = new umgr.AirSlideJoint
                    {
                        Joint = typeChar == 'S' ? Joint.C : Joint.D,
                        Height = hRaw * 2
                    };
                }

                break;
            case 'C':
                if (_lastParentNote is umgr.AirCrash)
                {
                    var hChar = payload.Length >= 4 ? payload[3] : '0';
                    var hRaw = UgcPayload.Base36(hChar);
                    if (hRaw < 0) hRaw = 0;
                    child = new umgr.AirCrashJoint { Height = hRaw * 2 };
                }

                break;
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
        ReportAtCurrentLine(Severity.Warning, string.Format(Strings.Mg_Unrecognized_note, what, 0));

    private void WarnUnknownType(char c) =>
        ReportAtCurrentLine(Severity.Warning, string.Format(Strings.Mg_Unrecognized_note, c.ToString(), 0));
}
