using PenguinTools.Core.Metadata;

namespace PenguinTools.Core.Chart.Models.mgxc;

public class Chart
{
    public Meta Meta { get; set; } = new();
    public Note Notes { get; set; } = new();
    public Event Events { get; set; } = new();

    public int GetLastTick(Func<Note, bool>? noteFilter = null)
    {
        var p = noteFilter != null ? Notes.Children.Where(noteFilter) : Notes.Children;
        return p.Select(note => note.GetLastTick()).Prepend(0).Max();
    }

    public TimeCalculator GetCalculator()
    {
        var beatEvents = Events.Children.OfType<BeatEvent>().OrderBy(e => e.Bar).ToList();
        var firstEvent = beatEvents.FirstOrDefault();
        if (firstEvent is not { Bar: 0 })
        {
            var newEvent = new BeatEvent { Bar = 0, Numerator = 4, Denominator = 4 };
            Events.InsertBefore(newEvent, firstEvent);
            beatEvents.Insert(0, newEvent);
        }
        return new TimeCalculator(Time.MarResolution, beatEvents);
    }
}