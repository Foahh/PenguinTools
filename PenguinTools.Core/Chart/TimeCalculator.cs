/*
   This code is modified from https://github.com/paralleltree/Ched
   Original Author: paralleltree
*/

using PenguinTools.Core.Chart.Models.mgxc;

namespace PenguinTools.Core.Chart;

    public class TimeCalculator
    {
        private readonly int _barTick;
        private readonly BeatEvent[] _timeSignatures;
        private readonly int[] _measureLengths;
        private readonly int[] _cumulativeBars;

        public TimeCalculator(int resolution, IEnumerable<BeatEvent> beatEvents)
        {
            _barTick = resolution;

            _timeSignatures = beatEvents.Where(e => e.Bar >= 0).OrderBy(x => x.Tick).ToArray();
            _measureLengths = new int[_timeSignatures.Length];
            _cumulativeBars = new int[_timeSignatures.Length];

            var barCount = 0;
            for (var i = 0; i < _timeSignatures.Length; i++)
            {
                var ts = _timeSignatures[i];
                _measureLengths[i] = GetMeasureLength(ts);

                if (i > 0)
                {
                    var prev = _timeSignatures[i - 1];
                    var ticksUnderCurrent = ts.Tick.Original - prev.Tick.Original;
                    var prevMeasureLength = _measureLengths[i - 1];
                    barCount += ticksUnderCurrent / prevMeasureLength;
                }
                _cumulativeBars[i] = barCount;
            }
        }

        public Position GetPositionFromTick(int tick)
        {
            var idx = FindTimeSignatureIndex(tick);
            var ts = _timeSignatures[idx];
            var measureLength = _measureLengths[idx];

            var delta = tick - ts.Tick.Original;
            var barsSince = delta / measureLength;
            var remainder = delta % measureLength;

            var totalBarsBefore = _cumulativeBars[idx];
            var beatTick = (double)_barTick / ts.Denominator;
            var beatIndex = (int)(remainder / beatTick);
            var tickOffset = (int)(remainder % beatTick);

            return new Position(totalBarsBefore + barsSince + 1, beatIndex + 1, tickOffset);
        }

        private int FindTimeSignatureIndex(int tick)
        {
            int low = 0, high = _timeSignatures.Length - 1;
            while (low <= high)
            {
                var mid = (low + high) / 2;
                if (_timeSignatures[mid].Tick.Original <= tick)
                {
                    if (mid == _timeSignatures.Length - 1 || _timeSignatures[mid + 1].Tick.Original > tick)
                        return mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
            throw new InvalidOperationException($"Tick {tick} is before all time signatures.");
        }

        private int GetMeasureLength(BeatEvent ts)
        {
            return (int)(_barTick / (double)ts.Denominator * ts.Numerator);
        }

        public readonly record struct Position(int BarIndex, int BeatIndex, int TickOffset)
        {
            public override string ToString() => $"{BarIndex}:{BeatIndex}.{TickOffset}";
        }
    }