/*
   This model is based on the original implementation from:
   https://margrithm.girlsband.party/
*/

using System.Text.Json.Serialization;

namespace PenguinTools.Core.Chart.Models.c2s;

public abstract class Event : Node;

public class Bpm : Event
{
    public decimal Value { get; set; }
    public override string Id => "BPM";
    [JsonIgnore] public override string Text => $"{base.Text}\t{Value:F3}";
}

public class Met : Event
{
    public int Numerator { get; set; }
    public int Denominator { get; set; }
    public override string Id => "MET";
    [JsonIgnore] public override string Text => $"{base.Text}\t{Denominator}\t{Numerator}";
}

public abstract class SpeedEventBase : Event
{
    public decimal Speed { get; set; }
    public Time Length { get; set; }
}

public class Slp : SpeedEventBase
{
    public virtual int Timeline { get; set; } = -1;
    public override string Id => "SLP";
    [JsonIgnore] public override string Text => $"{base.Text}\t{Length.Result}\t{Speed:F6}\t{Timeline}";
}


[Obsolete]
public class Sfl : SpeedEventBase
{
    public override string Id => "SFL";
    [JsonIgnore] public override string Text => $"{base.Text}\t{Length.Result}\t{Speed:F6}";
}

public class Dcm : SpeedEventBase
{
    public override string Id => "DCM";
    [JsonIgnore] public override string Text => $"{base.Text}\t{Length.Result}\t{Speed:F6}";
}