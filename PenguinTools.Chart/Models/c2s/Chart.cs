using PenguinTools.Core.Metadata;

namespace PenguinTools.Chart.Models.c2s;

public class Chart
{
    public Meta Meta { get; set; } = new();
    public List<Note> Notes { get; } = [];
    public List<Event> Events { get; } = [];
}