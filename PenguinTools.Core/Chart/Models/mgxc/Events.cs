namespace PenguinTools.Common.Chart.Models.mgxc;

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

public class SpeedEvent : Event
{
    public decimal Speed { get; set; }
}

public class TimelineEvent : SpeedEvent
{
    public int Timeline { get; set; }
}

public class NoteSpeedEvent : SpeedEvent;

public class BookmarkEvent : Event
{
    public string Tag { get; set; } = string.Empty;
}

public class BreakingMarker : Event;