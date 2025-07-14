/*
   This code is modified from https://github.com/paralleltree/Ched
   Original Author: paralleltree
*/

using PenguinTools.Core.Chart.Models.mgxc;

namespace PenguinTools.Core.Chart;

public class TimeCalculator
{
    public TimeCalculator(int resolution, IEnumerable<BeatEvent> beatEvents)
    {
        BarTick = resolution;
        TimeSignatures = beatEvents.Where(e => e.Bar >= 0).OrderBy(x => x.Tick).ToList();
    }

    private int BarTick { get; }
    private List<BeatEvent> TimeSignatures { get; }

    public Position GetPositionFromTick(int tick)
    {
        foreach (var ts in TimeSignatures.AsEnumerable().Reverse())
        {
            if (tick < ts.Tick.Original) continue;
            var measureLength = GetMeasureLength(ts);
            var delta = tick - ts.Tick.Original;
            var barsSinceThisSignature = delta / measureLength;
            var remainder = delta % measureLength;
            var totalBarsBefore = CalculateBarsBefore(ts);
            var beatTick = (double)BarTick / ts.Denominator;
            var beatIndex = (int)(remainder / beatTick);
            var tickOffset = (int)(remainder % beatTick);
            return new Position(totalBarsBefore + barsSinceThisSignature + 1, beatIndex + 1, tickOffset);
        }
        throw new InvalidOperationException();
    }

    private int CalculateBarsBefore(BeatEvent signature)
    {
        var barsCount = 0;
        var tss = TimeSignatures.ToList();
        for (var i = 0; i < tss.Count; i++)
        {
            var ts = tss[i];
            if (ts == signature) break;
            var measureLength = GetMeasureLength(ts);
            var nextTick = i < tss.Count - 1 ? tss[i + 1].Tick.Original : signature.Tick.Original;
            var ticksUnderCurrent = nextTick - ts.Tick.Original;
            barsCount += ticksUnderCurrent / measureLength;
        }
        return barsCount;
    }

    private int GetMeasureLength(BeatEvent ts)
    {
        return (int)(BarTick / (double)ts.Denominator * ts.Numerator);
    }

    public record Position(int BarIndex, int BeatIndex, int TickOffset)
    {
        public override string ToString()
        {
            return $"{BarIndex}:{BeatIndex}.{TickOffset}";
        }
    }
}