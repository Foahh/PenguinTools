using PenguinTools.Core.Chart.Models;

namespace PenguinTools.Core.Chart.Converter;

using mg = Models.mgxc;
using c2s = Models.c2s;

public partial class C2SConverter
{
    private void ConvertEvent(mg.Chart mgxc)
    {
        Time lastTick = mgxc.GetLastTick();

        var events = mgxc.Events.Children;
        foreach (var e in events.OfType<mg.BpmEvent>().OrderBy(e => e.Tick))
        {
            Events.Add(new c2s.Bpm
            {
                Tick = e.Tick,
                Value = e.Bpm
            });
        }

        foreach (var e in events.OfType<mg.BeatEvent>().OrderBy(e => e.Tick))
        {
            Events.Add(new c2s.Met
            {
                Tick = e.Tick,
                Numerator = e.Numerator,
                Denominator = e.Denominator
            });
        }
        
        ConvertDcm([..events.OfType<mg.NoteSpeedEvent>().OrderBy(e => e.Tick)], lastTick);
        ConvertSlp(mgxc, [..events.OfType<mg.ScrollSpeedEvent>().OrderBy(e => e.Tick)]);
    }

    private void ConvertDcm(List<mg.NoteSpeedEvent> events, Time lastTick)
    {
        if (events.Count <= 0) return;
        for (var i = 0; i < events.Count - 1; i++)
        {
            var curr = events[i];
            if (curr.Speed == 1m) continue;
            var next = events[i + 1];
            var note = new c2s.Dcm()
            {
                Tick = curr.Tick,
                Length = next.Tick.Round - curr.Tick.Round,
                Speed = curr.Speed
            };
            Events.Add(note);
        }

        var lastEvent = events[^1];
        if (lastEvent.Speed == 1m) return;
        var e = new c2s.Dcm
        {
            Tick = lastEvent.Tick,
            Length = Math.Max(lastTick.Round - lastEvent.Tick.Round, Time.SingleTick),
            Speed = lastEvent.Speed
        };
        Events.Add(e);
    }

    private void ConvertSlp(mg.Chart mgxc, List<mg.ScrollSpeedEvent> tilEvents)
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