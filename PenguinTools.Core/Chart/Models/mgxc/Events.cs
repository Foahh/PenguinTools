namespace PenguinTools.Core.Chart.Models.mgxc;

public class Event : TimeNode<Event>;

public class BpmEvent : Event
{
    public decimal Bpm { get; set; }
}

public class BeatEvent : Event
{
    public int Bar { get; set; }
    public int Denominator { get; set; } = 4;
    public int Numerator { get; set; } = 4;
}

public abstract class SpeedEventBase : Event
{
    public decimal Speed { get; set; }
}

public class ScrollSpeedEvent : SpeedEventBase
{
    public int Timeline { get; set; }
}

public class NoteSpeedEvent : SpeedEventBase;

public class BookmarkEvent : Event
{
    public string Tag { get; set; } = string.Empty;
}

public class BreakingMarker : Event;