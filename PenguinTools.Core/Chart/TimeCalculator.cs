/*
   This code is modified from https://github.com/paralleltree/Ched
   Original Author: paralleltree
*/

using PenguinTools.Common.Metadata;

namespace PenguinTools.Common.Chart;

public class TimeCalculator
{
    public TimeCalculator(int resolution, IEnumerable<TimeSignature> sigs)
    {
        BarTick = resolution;
        TimeSignatures = [.. sigs.OrderBy(x => x.Tick)];
        ReversedTimeSignatures = [.. TimeSignatures.Reverse()];
    }

    private int BarTick { get; }

    private IEnumerable<TimeSignature> TimeSignatures { get; }
    private IReadOnlyCollection<TimeSignature> ReversedTimeSignatures { get; }

    public Position GetPositionFromTick(int tick)
    {
        foreach (var ts in ReversedTimeSignatures)
        {
            if (tick < ts.Tick) continue;
            var measureLength = GetMeasureLength(ts);
            var delta = tick - ts.Tick;
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

    private int CalculateBarsBefore(TimeSignature signature)
    {
        var barsCount = 0;
        var listAsc = TimeSignatures.ToList();
        for (var i = 0; i < listAsc.Count; i++)
        {
            var current = listAsc[i];
            if (current == signature) break;
            var measureLength = GetMeasureLength(current);
            var nextTick = i < listAsc.Count - 1 ? listAsc[i + 1].Tick : signature.Tick;
            var ticksUnderCurrent = nextTick - current.Tick;
            barsCount += ticksUnderCurrent / measureLength;
        }
        return barsCount;
    }

    private int GetMeasureLength(TimeSignature ts)
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