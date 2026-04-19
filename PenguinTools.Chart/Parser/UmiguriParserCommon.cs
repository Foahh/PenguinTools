using PenguinTools.Core.Asset;
using PenguinTools.Core.Metadata;

namespace PenguinTools.Chart.Parser;

internal static class UmiguriParserCommon
{
    public const int DefaultBeatNumerator = 4;
    public const int DefaultBeatDenominator = 4;

    public static Difficulty DifficultyFromValue(int value) => value switch
    {
        0 => Difficulty.Basic,
        1 => Difficulty.Advanced,
        2 => Difficulty.Expert,
        3 => Difficulty.Master,
        4 => Difficulty.WorldsEnd,
        5 => Difficulty.Ultima,
        _ => Difficulty.Master
    };

    public static Entry CreateWorldsEndStage() => new(0, "WORLD'S END0001_ノイズ");

    public static string? FieldLineNameFromIndex(int index) => index switch
    {
        0 => "White",
        1 => "Red",
        2 => "Orange",
        3 => "Yellow",
        4 => "Olive",
        5 => "Green",
        6 => "SkyBlue",
        7 => "Blue",
        8 => "Purple",
        _ => null
    };
}
