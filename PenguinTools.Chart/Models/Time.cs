namespace PenguinTools.Chart.Models;

public readonly record struct Position(int Measure, int Offset);

public readonly record struct Time(int Original) : IComparable<Time>
{
    public int Round => (int)Math.Round((decimal)Original / ChartResolution.SingleTick) * ChartResolution.SingleTick;
    public int Scaled => (int)(Round * ChartResolution.TickFactor);
    public Position Position => new(Round / ChartResolution.UmiguriTick, (int)(Round % ChartResolution.UmiguriTick * ChartResolution.TickFactor));

    public int CompareTo(Time other) => Original.CompareTo(other.Original);

    public static implicit operator Time(int value) => new(value);
}
