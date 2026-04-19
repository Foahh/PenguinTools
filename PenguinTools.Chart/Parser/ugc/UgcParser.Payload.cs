using PenguinTools.Chart.Models;

namespace PenguinTools.Chart.Parser.ugc;

// Internal helpers exposed via a static class so tests can reach them without
// new'ing a whole UgcParser.
internal static class UgcPayload
{
    private const int TransparentCrashDensity = 0x7FFFFFFF;

    public static int Base36(char c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'a' && c <= 'z') return 10 + (c - 'a');
        if (c >= 'A' && c <= 'Z') return 10 + (c - 'A');
        return -1;
    }

    // UGC ExTap effect characters per Margrete v8 spec:
    // U=Up, D=Down, C=Center, A=RotateLeft (anticlockwise), W=RotateRight,
    // L=Left, R=Right, I=InOut (burst).
    public static ExEffect ExEffectChar(char c) => c switch
    {
        'U' => ExEffect.UP,
        'D' => ExEffect.DW,
        'C' => ExEffect.CE,
        'L' => ExEffect.RS,
        'R' => ExEffect.LS,
        'A' => ExEffect.RC,
        'W' => ExEffect.LC,
        'I' => ExEffect.BS,
        _ => ExEffect.UP   // caller should have validated; default for safety
    };

    public static int Base36(ReadOnlySpan<char> chars)
    {
        if (chars.Length == 0) return -1;
        var value = 0;
        foreach (var c in chars)
        {
            var digit = Base36(c);
            if (digit < 0) return -1;
            value = value * 36 + digit;
        }

        return value;
    }

    public static decimal Height36(ReadOnlySpan<char> chars)
    {
        var raw = Base36(chars);
        return raw < 0 ? -1 : raw;
    }

    public static AirDirection AirDirectionCode(ReadOnlySpan<char> code) => code.ToString().ToUpperInvariant() switch
    {
        "UC" => AirDirection.IR,
        "UL" => AirDirection.UR,
        "UR" => AirDirection.UL,
        "DC" => AirDirection.DW,
        "DL" => AirDirection.DR,
        "DR" => AirDirection.DL,
        _ => AirDirection.IR
    };

    // Air color: second-char of payload suffix; normal/pink toggle.
    public static Color AirColorChar(char c) => c switch
    {
        'N' => Color.DEF,   // normal
        'I' => Color.PNK,   // inverted
        _ => Color.DEF
    };

    public static int AirCrashInterval(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        if (s == "$") return TransparentCrashDensity;
        return int.TryParse(s, out var density) ? density : 0;
    }

    public static Color CrushColorChar(char c) => c switch
    {
        '0' => Color.DEF,
        '1' => Color.RED,
        '2' => Color.ORN,
        '3' => Color.YEL,
        '4' => Color.LIM,
        '5' => Color.GRN,
        '6' => Color.AQA,
        '7' => Color.CYN,
        '8' => Color.DGR,
        '9' => Color.BLU,
        'A' => Color.VLT,
        'Y' => Color.PPL,
        'B' => Color.PNK,
        'C' => Color.GRY,
        'D' => Color.BLK,
        'Z' => Color.NON,
        _ => Color.DEF
    };
}
