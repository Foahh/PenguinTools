using PenguinTools.Core.Resources;

// ReSharper disable RedundantNameQualifier

namespace PenguinTools.Core.Chart.Writer;

using mg = Models.mgxc;
using c2s = Models.c2s;

public partial class C2SChartWriter
{
    private readonly Dictionary<mg.NegativeNote, c2s.IPairable> _negativePairRoots = [];
    private readonly Dictionary<mg.PositiveNote, c2s.Note> _positivePairTargets = [];

    private T CreateNote<TSource, T>(TSource source, Action<T>? action = null) where TSource : mg.Note where T : c2s.Note, new()
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

        return note;
    }

    private T CreatePositiveNote<TSource, T>(TSource source, Action<T>? action = null)
        where TSource : mg.PositiveNote where T : c2s.Note, new()
    {
        var note = CreateNote<TSource, T>(source, action);
        RegisterPositivePairTarget(source, note);
        return note;
    }

    private void RegisterPositivePairTarget(mg.PositiveNote source, c2s.Note target)
    {
        _positivePairTargets[source] = target;
    }

    private void RegisterNegativePairRoot(mg.NegativeNote source, c2s.IPairable target)
    {
        _negativePairRoots[source] = target;
    }

    private void ResolvePairings()
    {
        foreach (var (source, root) in _negativePairRoots)
        {
            if (source.PairNote is null) continue;
            if (_positivePairTargets.TryGetValue(source.PairNote, out var parent))
            {
                root.Parent = parent;
            }
        }
    }

    private void ConvertNote(mg.Note e)
    {
        switch (e)
        {
            case mg.SoflanArea sla:
                ProcessSoflanArea(sla);
                break;
            case mg.Tap tap:
                CreatePositiveNote<mg.Tap, c2s.Tap>(tap);
                break;
            case mg.ExTap exTap:
                CreatePositiveNote<mg.ExTap, c2s.ExTap>(exTap, x => x.Effect = exTap.Effect);
                break;
            case mg.Flick flick:
                CreatePositiveNote<mg.Flick, c2s.Flick>(flick);
                break;
            case mg.Damage damage:
                CreatePositiveNote<mg.Damage, c2s.Damage>(damage);
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
        var joints = airCrash.Children.OfType<mg.AirCrashJoint>().Prepend(airCrash.AsChild()).ToArray();

        var density = airCrash.Density;
        if (density.Original >= 0x7FFFFFFF) density = (airCrash.GetLastTick() - airCrash.Tick.Original) * 2;

        for (var i = 0; i < joints.Length - 1; i++)
        {
            var curr = joints[i];
            var next = joints[i + 1];
            CreateNote<mg.AirCrashJoint, c2s.AirCrash>(curr, x =>
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
        if (airSlide.PairNote?.PairNote != airSlide) throw new DiagnosticException(Strings.MgCrit_Invalid_AirSlide_parent, airSlide, airSlide.Tick.Original);

        var joints = airSlide.Children.OfType<mg.AirSlideJoint>().Prepend(airSlide.AsChild()).ToArray();
        c2s.AirSlide? firstSegment = null;
        c2s.Note? previousSegment = null;
        for (var i = 0; i < joints.Length - 1; i++)
        {
            var curr = joints[i];
            var next = joints[i + 1];
            var segment = CreateNote<mg.AirSlideJoint, c2s.AirSlide>(curr, x =>
            {
                x.Parent = previousSegment;
                x.Color = airSlide.Color;
                x.Height = curr.Height;
                x.Joint = next.Joint;
                x.EndTick = next.Tick;
                x.EndLane = next.Lane;
                x.EndWidth = next.Width;
                x.EndHeight = next.Height;
            });
            firstSegment ??= segment;
            previousSegment = segment;
        }

        if (firstSegment != null)
        {
            RegisterNegativePairRoot(airSlide, firstSegment);
        }
    }

    private void ProcessAir(mg.Air airNote)
    {
        if (airNote.PairNote?.PairNote != airNote) throw new DiagnosticException(Strings.MgCrit_Invalid_Air_parent, airNote, airNote.Tick.Original);

        var note = CreateNote<mg.Air, c2s.Air>(airNote, x =>
        {
            x.Direction = airNote.Direction;
            x.Color = airNote.Color;
        });
        RegisterNegativePairRoot(airNote, note);
    }

    private void ProcessSlide(mg.Slide slide)
    {
        var joints = slide.Children.OfType<mg.SlideJoint>().Prepend(slide.AsChild()).ToArray();
        for (var i = 0; i < joints.Length - 1; i++)
        {
            var curr = joints[i];
            var next = joints[i + 1];
            var index = i;
            var note = CreateNote<mg.SlideJoint, c2s.Slide>(curr, x =>
            {
                x.Joint = next.Joint;
                x.EndTick = next.Tick;
                x.EndLane = next.Lane;
                x.EndWidth = next.Width;
                x.Effect = index == 0 ? slide.Effect : null;
            });
            // pair the last joint with air
            if (i == joints.Length - 2)
            {
                RegisterPositivePairTarget(next, note);
            }
        }
    }

    private void ProcessSoflanArea(mg.SoflanArea sla)
    {
        if (sla.LastChild is not mg.SoflanAreaJoint tail) throw new DiagnosticException(Strings.MgCrit_SoflanArea_has_no_tail, sla, sla.Tick.Original);

        CreateNote<mg.SoflanArea, c2s.Sla>(sla, x =>
        {
            x.Length = tail.Tick.Round - sla.Tick.Round;
        });
    }

    private void ProcessHold(mg.Hold hold)
    {
        if (hold.LastChild is not mg.HoldJoint tail) throw new DiagnosticException(Strings.MgCrit_Hold_has_no_tail, hold, hold.Tick.Original);

        var note = CreateNote<mg.Hold, c2s.Hold>(hold, x =>
        {
            x.EndTick = tail.Tick;
            x.Effect = hold.Effect;
        });
        RegisterPositivePairTarget(tail, note);
    }
}
