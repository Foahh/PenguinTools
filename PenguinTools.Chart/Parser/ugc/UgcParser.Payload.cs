using PenguinTools.Chart.Models;

namespace PenguinTools.Chart.Parser;

// Internal helpers exposed via a static class so tests can reach them without
// new'ing a whole UgcParser.
internal static class UgcPayload
{
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
        'L' => ExEffect.LS,
        'R' => ExEffect.RS,
        'A' => ExEffect.LC,
        'W' => ExEffect.RC,
        'I' => ExEffect.BS,
        _ => ExEffect.UP   // caller should have validated; default for safety
    };

    // Air direction: first-char of payload suffix per spec.
    // Normal = top-of-air (IR = inverse-raise = straight up), pink variant flagged by color.
    public static AirDirection AirDirectionChar(char c) => c switch
    {
        'U' => AirDirection.IR,   // straight up
        'D' => AirDirection.DW,   // straight down
        'N' => AirDirection.IR,   // "normal" = up; used for AirHold/AirSlide/AirCrush
        'L' => AirDirection.UL,   // up-left
        'R' => AirDirection.UR,   // up-right
        'K' => AirDirection.DL,   // down-left (spec calls this "kL")
        'J' => AirDirection.DR,   // down-right
        _ => AirDirection.IR
    };

    // Air color: second-char of payload suffix; normal/pink toggle.
    public static Color AirColorChar(char c) => c switch
    {
        'N' => Color.DEF,   // normal
        'P' => Color.PNK,   // pink
        _ => Color.DEF
    };

    // AirCrush color character (C-note). Mirrors MgxcParser.Note.cs variationId table.
    public static Color CrushColorChar(char c) => c switch
    {
        '0' => Color.DEF,
        '1' => Color.RED,
        '2' => Color.ORN,
        '3' => Color.YEL,
        '4' => Color.GRN,
        '5' => Color.AQA,
        '6' => Color.BLU,
        '7' => Color.PPL,
        '8' => Color.VLT,
        '9' => Color.PPL,
        'a' => Color.GRY,
        'b' => Color.BLK,
        'c' => Color.LIM,
        'd' => Color.CYN,
        'e' => Color.DGR,
        'f' => Color.PNK,
        'x' => Color.NON,
        _ => Color.DEF
    };
}
