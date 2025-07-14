/*
   This model is based on the original implementation from:
   https://margrithm.girlsband.party/
*/

using PenguinTools.Core.Resources;
using System.Text.Json.Serialization;

namespace PenguinTools.Core.Chart.Models.c2s;

public class Tap : Note
{
    public override string Id => "TAP";
}

public class Damage : Note
{
    public override string Id => "MNE";
}

public class Flick : Note
{
    public override string Id => "FLK";
}

public class ExTap : Note
{
    public ExEffect? Effect { get; set; }
    public override string Id => "CHR";
    [JsonIgnore] public override string Text => $"{base.Text}{Effect.GetKind()}";
}

public class Hold : ExTapableLongNote
{
    public override string Id => $"H{Effect.GetMark()}D";
    [JsonIgnore] public override string Text => $"{base.Text}\t{Length.Result}{Effect.GetKind()}";
}

public class Sla : Note
{
    public Time Length { get; set; }
    public override string Id => "SLA";
    [JsonIgnore] public override string Text => $"{base.Text}\t{Length.Result}\t{Timeline}";
}

public class Slide : ExTapableLongNote
{
    public Joint Joint { get; set; }
    public override string Id => $"S{Effect.GetMark()}{Joint}";
    [JsonIgnore] public override string Text => $"{base.Text}\t{Length.Result}\t{EndLane}\t{EndWidth}\tSLD{Effect.GetKind()}";
}

public interface IPairable
{
    public Note? Parent { get; set; }
    public string ParentId { get; }
}

public class Air : Note, IPairable
{
    public AirDirection Direction { get; set; }
    public Color Color { get; set; } = Color.DEF;

    public override string Id => $"A{Direction}";
    [JsonIgnore] public override string Text => $"{base.Text}\t{ParentId}\t{Color}";
    public Note? Parent { get; set; }
    [JsonIgnore] public string ParentId => Parent?.Id ?? throw new DiagnosticException(Strings.Error_air_parent_null, this, Tick.Original);
}

public class AirSlide : LongHeightNote, IPairable
{
    public Joint Joint { get; set; }
    public Color Color { get; set; } = Color.DEF;

    public override string Id => $"AS{Joint}";
    [JsonIgnore] public override string Text => $"{base.Text}\t{ParentId}\t{Height.Result:F1}\t{Length.Result}\t{EndLane}\t{EndWidth}\t{EndHeight.Result:F1}\t{Color}";
    public Note? Parent { get; set; }
    [JsonIgnore] public string ParentId => Parent?.Id ?? throw new DiagnosticException(Strings.Error_air_slide_parent_null, this, Tick.Original);
}

public class AirCrash : LongHeightNote
{
    public Time Density { get; set; }
    public Color Color { get; set; } = Color.DEF;

    public override string Id => "ALD";
    [JsonIgnore] public override string Text => $"{base.Text}\t{Density.Result}\t{Height.Result:F1}\t{Length.Result}\t{EndLane}\t{EndWidth}\t{EndHeight.Result:F1}\t{Color}";
}