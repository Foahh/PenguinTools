namespace PenguinTools.Chart.Models;

public static class ChartResolution
{
    public const int MarResolution = 1920;
    public const int CtsResolution = 384;
    public const int SingleTick = MarResolution / CtsResolution;
    public const decimal TickFactor = (decimal)CtsResolution / MarResolution;
}
