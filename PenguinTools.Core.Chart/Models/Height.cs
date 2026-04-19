/*
   This code is based on the original implementation from:
   https://github.com/inonote/MargreteOnline
*/

namespace PenguinTools.Core.Chart.Models;

public readonly record struct Height(decimal Original) : IComparable<Height>
{
    public decimal Result => Math.Round(Math.Max(0m, Original / 10m * 0.5m + 1m), 1);

    public int CompareTo(Height other) => Original.CompareTo(other.Original);

    public static Height operator -(Height a, Height b) => a.Original - b.Original;

    public static implicit operator Height(decimal value) => new(value);
}
