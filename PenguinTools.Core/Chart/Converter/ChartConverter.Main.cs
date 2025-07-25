using PenguinTools.Core.Resources;

// ReSharper disable RedundantNameQualifier

namespace PenguinTools.Core.Chart.Converter;

using mg = Models.mgxc;
using c2s = Models.c2s;

public partial class ChartConverter
{
    private readonly Dictionary<mg.NegativeNote, c2s.Note> nMap = [];
    private readonly Dictionary<mg.PositiveNote, c2s.Note> pMap = [];

    private void TryPairingNegative(mg.PositiveNote source)
    {
        if (source.PairNote != null && nMap.TryGetValue(source.PairNote, out var pair) && pair is c2s.IPairable children)
        {
            children.Parent = pair;
        }
    }

    private void TryPairingPositive(mg.NegativeNote source)
    {
        if (source.PairNote != null && pMap.TryGetValue(source.PairNote, out var pair) && pair is c2s.IPairable children)
        {
            children.Parent = pair;
        }
    }

    private T CreateNote<TSource, T>(Dictionary<TSource, c2s.Note> noteMap, TSource source, Action<T>? action = null) where TSource : mg.Note where T : c2s.Note, new()
    {
        var note = new T
        {
            Timeline = source.Timeline,
            Tick = source.Tick,
            Lane = source.Lane,
            Width = source.Width
        };

        action?.Invoke(note);
        Notes.Add(note);
        noteMap[source] = note;

        if (source is mg.PositiveNote pNote) TryPairingNegative(pNote);
        else if (source is mg.NegativeNote nNote) TryPairingPositive(nNote);

        return note;
    }

    private T CreateNote<T>(mg.Note source, Action<T>? extra = null) where T : c2s.Note, new()
    {
        var note = new T
        {
            Timeline = source.Timeline,
            Tick = source.Tick,
            Lane = source.Lane,
            Width = source.Width
        };
        extra?.Invoke(note);
        Notes.Add(note);
        return note;
    }

    private void ConvertNote(mg.Note e)
    {
        switch (e)
        {
            case mg.SoflanArea sla:
                ProcessSoflanArea(sla);
                break;
            case mg.Tap tap:
                CreateNote<mg.PositiveNote, c2s.Tap>(pMap, tap);
                break;
            case mg.ExTap exTap:
                CreateNote<mg.PositiveNote, c2s.ExTap>(pMap, exTap, x => x.Effect = exTap.Effect);
                break;
            case mg.Flick flick:
                CreateNote<mg.PositiveNote, c2s.Flick>(pMap, flick);
                break;
            case mg.Damage damage:
                CreateNote<mg.PositiveNote, c2s.Damage>(pMap, damage);
                break;
            case mg.Hold hold:
                ProcessHold(hold);
                break;
            case mg.Slide slide:
                ProcessSlide(slide);
                break;
            case mg.Air airNote:
                ProcessAir(airNote);
                break;
            case mg.AirSlide airSlide:
                ProcessAirSlide(airSlide);
                break;
            case mg.AirCrash airCrash:
                ProcessAirCrash(airCrash);
                break;
        }
    }

    private void ProcessAirCrash(mg.AirCrash airCrash)
    {
        var joints = airCrash.Children.OfType<mg.AirCrashJoint>().Prepend(airCrash.AsChild()).ToList();

        var density = airCrash.Density;
        if (density.Original >= 0x7FFFFFFF) density = (airCrash.GetLastTick() - airCrash.Tick.Original) * 2;

        for (var i = 0; i < joints.Count - 1; i++)
        {
            var curr = joints[i];
            var next = joints[i + 1];
            CreateNote<c2s.AirCrash>(curr, x =>
            {
                x.EndTick = next.Tick;
                x.EndLane = next.Lane;
                x.EndWidth = next.Width;
                x.Height = curr.Height;
                x.EndHeight = next.Height;
                x.Color = airCrash.Color;
                x.Density = density;
            });
        }
    }

    private void ProcessAirSlide(mg.AirSlide airSlide)
    {
        if (airSlide.PairNote?.PairNote != airSlide) throw new DiagnosticException(Strings.Error_invalid_AirSlide_parent, airSlide, airSlide.Tick.Original);

        var parent = pMap.GetValueOrDefault(airSlide.PairNote);
        var joints = airSlide.Children.OfType<mg.AirSlideJoint>().Prepend(airSlide.AsChild()).ToList();
        for (var i = 0; i < joints.Count - 1; i++)
        {
            var prev = parent;
            var curr = joints[i];
            var next = joints[i + 1];
            parent = CreateNote<c2s.AirSlide>(curr, x =>
            {
                x.Parent = prev;
                x.Color = airSlide.Color;
                x.Height = curr.Height;
                x.Joint = next.Joint;
                x.EndTick = next.Tick;
                x.EndLane = next.Lane;
                x.EndWidth = next.Width;
                x.EndHeight = next.Height;
            });

            // pair the first joint with ground note
            if (i == 0)
            {
                nMap[airSlide] = parent;
                TryPairingPositive(airSlide);
            }
        }
    }

    private void ProcessAir(mg.Air airNote)
    {
        if (airNote.PairNote?.PairNote != airNote) throw new DiagnosticException(Strings.Error_invalid_Air_parent, airNote, airNote.Tick.Original);

        var note = CreateNote<mg.NegativeNote, c2s.Air>(nMap, airNote, x =>
        {
            x.Parent = pMap.GetValueOrDefault(airNote.PairNote);
            x.Direction = airNote.Direction;
            x.Color = airNote.Color;
        });
        nMap[airNote] = note;
    }

    private void ProcessSlide(mg.Slide slide)
    {
        var joints = slide.Children.OfType<mg.SlideJoint>().Prepend(slide.AsChild()).ToList();
        for (var i = 0; i < joints.Count - 1; i++)
        {
            var curr = joints[i];
            var next = joints[i + 1];
            var index = i;
            var note = CreateNote<c2s.Slide>(curr, x =>
            {
                x.Joint = next.Joint;
                x.EndTick = next.Tick;
                x.EndLane = next.Lane;
                x.EndWidth = next.Width;
                x.Effect = index == 0 ? slide.Effect : null;
            });
            // pair the last joint with air
            if (i == joints.Count - 2)
            {
                pMap[next] = note;
                TryPairingNegative(next);
            }
        }
    }

    private void ProcessSoflanArea(mg.SoflanArea sla)
    {
        if (sla.LastChild is not mg.SoflanAreaJoint tail) throw new DiagnosticException(Strings.Error_soflanArea_has_no_tail, sla, sla.Tick.Original);

        CreateNote<c2s.Sla>(sla, x =>
        {
            x.Length = tail.Tick.Round - sla.Tick.Round;
        });
    }

    private void ProcessHold(mg.Hold hold)
    {
        if (hold.LastChild is not mg.HoldJoint tail) throw new DiagnosticException(Strings.Error_hold_has_no_tail, hold, hold.Tick.Original);

        var note = CreateNote<c2s.Hold>(hold, x =>
        {
            x.EndTick = tail.Tick;
            x.Effect = hold.Effect;
        });
        pMap[tail] = note;
        TryPairingNegative(tail);
    }


}