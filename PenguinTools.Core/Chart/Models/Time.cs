namespace PenguinTools.Core.Chart.Models;

public readonly record struct Position(int Measure, int Offset);

public readonly record struct Time(int Original) : IComparable<Time>
{
    public const int MarResolution = 1920;
    public const int CtsResolution = 384;
    public const int SingleTick = MarResolution / CtsResolution;
    private const decimal Factor = (decimal)CtsResolution / MarResolution;

    public int Round { get; } = (int)Math.Round((decimal)Original / SingleTick) * SingleTick;
    public int Result => (int)(Round * Factor);

    public Position Position => new(Round / MarResolution, (int)(Round % MarResolution * Factor));

    # region IComparable

    public int CompareTo(Time other)
    {
        return Original.CompareTo(other.Original);
    }

    #endregion

    public static implicit operator Time(int value)
    {
        return new Time(value);
    }
}