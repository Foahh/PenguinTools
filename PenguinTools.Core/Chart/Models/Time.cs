namespace PenguinTools.Core.Chart.Models;

public readonly record struct Position(int Measure, int Offset);

public readonly struct Time(int original) : IComparable<Time>, IEquatable<Time>
{
    public const int MarResolution = 1920;
    public const int CtsResolution = 384;
    public const int SingleTick = MarResolution / CtsResolution;
    private const decimal Factor = (decimal)CtsResolution / MarResolution;

    public int Original { get; } = original;
    public int Round { get; } = (int)Math.Round((decimal)original / SingleTick) * SingleTick;
    public int Scaled => (int)(Round * Factor);
    public Position Position => new(Round / MarResolution, (int)(Round % MarResolution * Factor));

    public int CompareTo(Time other)
    {
        return Original.CompareTo(other.Original);
    }
    
    public static implicit operator Time(int value)
    {
        return new Time(value);
    }

    public bool Equals(Time other)
    {
        return Original == other.Original;
    }

    public override bool Equals(object? obj)
    {
        return obj is Time other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Original);
    }

    public static bool operator ==(Time left, Time right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Time left, Time right)
    {
        return !(left == right);
    }
}