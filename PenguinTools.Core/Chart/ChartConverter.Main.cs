using PenguinTools.Common.Chart.Models;
using PenguinTools.Common.Resources;

// ReSharper disable RedundantNameQualifier

namespace PenguinTools.Common.Chart;

using mgxc = Models.mgxc;
using c2s = Models.c2s;

public partial class ChartConverter
{
    private readonly Dictionary<mgxc.NegativeNote, c2s.Note> nMap = [];
    private readonly Dictionary<mgxc.PositiveNote, c2s.Note> pMap = [];

    private void TryPairingNegative(mgxc.PositiveNote source)
    {
        if (source.PairNote != null && nMap.TryGetValue(source.PairNote, out var pair) && pair is c2s.IPairable children)
        {
            children.Parent = pair;
        }
    }

    private void TryPairingPositive(mgxc.NegativeNote source)
    {
        if (source.PairNote != null && pMap.TryGetValue(source.PairNote, out var pair) && pair is c2s.IPairable children)
        {
            children.Parent = pair;
        }
    }

    private T CreateNote<TSource, T>(Dictionary<TSource, c2s.Note> noteMap, TSource source, Action<T>? action = null) where TSource : mgxc.Note where T : c2s.Note, new()
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

        if (source is mgxc.PositiveNote pNote) TryPairingNegative(pNote);
        else if (source is mgxc.NegativeNote nNote) TryPairingPositive(nNote);

        return note;
    }

    private T CreateNote<T>(mgxc.Note source, Action<T>? extra = null) where T : c2s.Note, new()
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

    private void ConvertNote(mgxc.Note e)
    {
        switch (e)
        {
            case mgxc.SoflanArea sla:
                ProcessSoflanArea(sla);
                break;
            case mgxc.Tap tap:
                CreateNote<mgxc.PositiveNote, c2s.Tap>(pMap, tap);
                break;
            case mgxc.ExTap exTap:
                CreateNote<mgxc.PositiveNote, c2s.ExTap>(pMap, exTap, x => x.Effect = exTap.Effect);
                break;
            case mgxc.Flick flick:
                CreateNote<mgxc.PositiveNote, c2s.Flick>(pMap, flick);
                break;
            case mgxc.Damage damage:
                CreateNote<mgxc.PositiveNote, c2s.Damage>(pMap, damage);
                break;
            case mgxc.Hold hold:
                ProcessHold(hold);
                break;
            case mgxc.Slide slide:
                ProcessSlide(slide);
                break;
            case mgxc.Air airNote:
                ProcessAir(airNote);
                break;
            case mgxc.AirSlide airSlide:
                ProcessAirSlide(airSlide);
                break;
            case mgxc.AirCrash airCrash:
                ProcessAirCrash(airCrash);
                break;
        }
    }

    private void ProcessAirCrash(mgxc.AirCrash airCrash)
    {
        var joints = airCrash.Children.OfType<mgxc.AirCrashJoint>().Prepend(airCrash.AsChild()).ToList();

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

    private void ProcessAirSlide(mgxc.AirSlide airSlide)
    {
        if (airSlide.PairNote?.PairNote != airSlide) throw new DiagnosticException(Strings.Error_invalid_AirSlide_parent, airSlide, airSlide.Tick.Original);

        var parent = pMap.GetValueOrDefault(airSlide.PairNote);
        var joints = airSlide.Children.OfType<mgxc.AirSlideJoint>().Prepend(airSlide.AsChild()).ToList();
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

    private void ProcessAir(mgxc.Air airNote)
    {
        if (airNote.PairNote?.PairNote != airNote) throw new DiagnosticException(Strings.Error_invalid_Air_parent, airNote, airNote.Tick.Original);

        var note = CreateNote<mgxc.NegativeNote, c2s.Air>(nMap, airNote, x =>
        {
            x.Parent = pMap.GetValueOrDefault(airNote.PairNote);
            x.Direction = airNote.Direction;
            x.Color = airNote.Color;
        });
        nMap[airNote] = note;
    }

    private void ProcessSlide(mgxc.Slide slide)
    {
        var joints = slide.Children.OfType<mgxc.SlideJoint>().Prepend(slide.AsChild()).ToList();
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

    private void ProcessSoflanArea(mgxc.SoflanArea sla)
    {
        if (sla.LastChild is not mgxc.SoflanAreaJoint tail) throw new DiagnosticException(Strings.Error_soflanArea_has_no_tail, sla, sla.Tick.Original);

        CreateNote<c2s.Sla>(sla, x =>
        {
            x.Length = tail.Tick.Round - sla.Tick.Round;
        });
    }

    private void ProcessHold(mgxc.Hold hold)
    {
        if (hold.LastChild is not mgxc.HoldJoint tail) throw new DiagnosticException(Strings.Error_hold_has_no_tail, hold, hold.Tick.Original);

        var note = CreateNote<c2s.Hold>(hold, x =>
        {
            x.EndTick = tail.Tick;
            x.Effect = hold.Effect;
        });
        pMap[tail] = note;
        TryPairingNegative(tail);
    }


    private void ConvertEvent(mgxc.Chart mgxc)
    {
        Time lastTick = mgxc.GetLastTick();

        var events = mgxc.Events.Children;
        foreach (var e in events.OfType<mgxc.BpmEvent>().OrderBy(e => e.Tick))
        {
            Events.Add(new c2s.Bpm
            {
                Tick = e.Tick,
                Value = e.Bpm
            });
        }

        foreach (var e in events.OfType<mgxc.BeatEvent>().OrderBy(e => e.Tick))
        {
            Events.Add(new c2s.Met
            {
                Tick = e.Tick,
                Numerator = e.Numerator,
                Denominator = e.Denominator
            });
        }

        ConvertSfl([.. events.OfType<mgxc.NoteSpeedEvent>().OrderBy(e => e.Tick)], lastTick);
        ConvertSlp(mgxc, [.. events.OfType<mgxc.TimelineEvent>().OrderBy(e => e.Tick)]);
    }

    private void ConvertSfl(List<mgxc.NoteSpeedEvent> events, Time lastTick)
    {
        if (events.Count <= 0) return;
        for (var i = 0; i < events.Count - 1; i++)
        {
            var curr = events[i];
            if (curr.Speed == 1m) continue;
            var next = events[i + 1];
            var note = new c2s.Sfl
            {
                Tick = curr.Tick,
                Length = next.Tick.Round - curr.Tick.Round,
                Speed = curr.Speed
            };
            Events.Add(note);
        }

        var lastEvent = events[^1];
        if (lastEvent.Speed == 1m) return;
        var e = new c2s.Sfl
        {
            Tick = lastEvent.Tick,
            Length = Math.Max(lastTick.Round - lastEvent.Tick.Round, Time.SingleTick),
            Speed = lastEvent.Speed
        };
        Events.Add(e);
    }

    private void ConvertSlp(mgxc.Chart mgxc, List<mgxc.TimelineEvent> tilEvents)
    {
        if (tilEvents.Count <= 0) return;
        var tilGroups = tilEvents.GroupBy(til => til.Timeline).ToDictionary(g => g.Key, g => g.ToList());
        var convertSlp = new List<c2s.Slp>();

        foreach (var (id, grouped) in tilGroups)
        {
            Time lastTilTick = mgxc.GetLastTick(p => p.Timeline == id);

            if (grouped.Count <= 0) continue;
            for (var i = 0; i < grouped.Count - 1; i++)
            {
                var curr = grouped[i];
                var next = grouped[i + 1];
                convertSlp.Add(new c2s.Slp
                {
                    Timeline = id,
                    Tick = curr.Tick,
                    Length = next.Tick.Round - curr.Tick.Round,
                    Speed = curr.Speed
                });
            }
            var lastEvent = grouped[^1];
            convertSlp.Add(new c2s.Slp
            {
                Timeline = id,
                Tick = lastEvent.Tick,
                Length = Math.Max(lastTilTick.Round - lastEvent.Tick.Round, Time.SingleTick),
                Speed = lastEvent.Speed
            });
        }

        convertSlp.RemoveAll(e => e.Speed == 1m);
        Events.AddRange(convertSlp);
    }
}