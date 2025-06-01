using PenguinTools.Common.Metadata;

namespace PenguinTools.Common.Chart.Models.mgxc;

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
        if (beatEvents.FirstOrDefault()?.Bar != 0)
        {
            Events.InsertBefore(new BeatEvent { Bar = 0, Numerator = 4, Denominator = 4 }, beatEvents.FirstOrDefault());
            beatEvents = [.. Events.Children.OfType<BeatEvent>().OrderBy(e => e.Bar)];
        }
        return new TimeCalculator(Time.MarResolution, beatEvents.Select(e => new TimeSignature(e.Tick.Original, e.Numerator, e.Denominator)));
    }
}