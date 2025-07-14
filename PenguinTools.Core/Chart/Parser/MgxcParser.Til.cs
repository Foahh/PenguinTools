using PenguinTools.Core.Chart.Models;
using PenguinTools.Core.Resources;
using mgxc = PenguinTools.Core.Chart.Models.mgxc;

namespace PenguinTools.Core.Chart.Parser;

public partial class MgxcParser
{
    private readonly Dictionary<int, List<mgxc.Note>> noteGroups = [];
    private readonly Dictionary<int, List<mgxc.ScrollSpeedEvent>> tilGroups = [];

    // thanks to @tångent 90°
    protected void ProcessTil()
    {
        GroupEventByTimeline(mgxc.Events);
        GroupNoteByTimeline(mgxc.Notes);
        MoveMainTimeline(mgxc.Meta.MainTil);
        ClearEmptyGroups();
        // TODO: Find conflicting note, compare priority and put them in separate group (SLA with larger TIL => larger priority when applying on note)
        PlaceSoflanArea();
        FinalizeEvent();
        FindNoteViolations();

        tilGroups.Clear();
        noteGroups.Clear();
    }

    private void FinalizeEvent()
    {
        foreach (var e in mgxc.Events.Children.OfType<mgxc.SpeedEventBase>().ToList()) mgxc.Events.RemoveChild(e);
        foreach (var (tilId, events) in tilGroups)
        {
            foreach (var e in events)
            {
                var newEvent = new mgxc.ScrollSpeedEvent
                {
                    Tick = e.Tick,
                    Timeline = tilId,
                    Speed = e.Speed
                };
                mgxc.Events.AppendChild(newEvent);
            }
        }
    }

    private void PlaceSoflanArea()
    {

        foreach (var tils in tilGroups.Values.ToList()) tils.Sort((a, b) => a.Tick.CompareTo(b.Tick));
        var slaSet = new HashSet<(int Tick, int Timeline, int Lane, int Width)>();
        foreach (var (id, notes) in noteGroups)
        {
            if (id == 0) continue;
            var events = tilGroups[id];
            foreach (var note in notes)
            {
                note.Timeline = id;

                // magic optimization: when the crash is transparent, it is not necessary to add the SLA on the control joint
                if (note is mgxc.AirCrashJoint { Parent: mgxc.AirCrash { Color: Color.Non }, Density.Original: 0x7FFFFFFF or 0 }) continue;

                // find the speed that is just before the note
                var prevTil = events.Where(p => p.Tick.Original <= note.Tick.Original).OrderByDescending(p => p.Tick).FirstOrDefault();
                if (prevTil?.Speed is null) continue;
                if (slaSet.Contains((note.Tick.Original, id, note.Lane, note.Width))) continue;

                var head = new mgxc.SoflanArea
                {
                    Tick = note.Tick,
                    Timeline = id,
                    Lane = note.Lane,
                    Width = note.Width
                };
                var tail = new mgxc.SoflanAreaJoint { Tick = note.Tick.Original + Time.SingleTick };

                slaSet.Add((note.Tick.Original, id, note.Lane, note.Width));
                head.AppendChild(tail);
                mgxc.Notes.AppendChild(head);
            }
        }
    }

    protected void GroupEventByTimeline(mgxc.Event events)
    {
        foreach (var til in events.Children.OfType<mgxc.ScrollSpeedEvent>())
        {
            var timelineId = til.Timeline;
            CreateGroup(timelineId);
            tilGroups[timelineId].Add(til);
        }
    }

    protected void GroupNoteByTimeline(mgxc.Note parent)
    {
        if (parent.Children.Count == 0) return;
        foreach (var note in parent.Children)
        {
            GroupNoteByTimeline(note);
            var timeline = note.Timeline;
            CreateGroup(timeline);
            noteGroups[timeline].Add(note);
        }
    }

    protected void MoveMainTimeline(int mainTil)
    {
        if (!tilGroups.ContainsKey(mainTil))
        {
            var msg = string.Format(Strings.Diag_main_timeline_not_found, mgxc.Meta.MainTil);
            diag.Report(Severity.Information, msg);
            return;
        }
        SwapGroup(mainTil, 0);
    }

    protected void ClearEmptyGroups()
    {
        foreach (var (id, events) in tilGroups.ToList())
        {
            var mappedNotes = noteGroups[id];
            var maxTick = mappedNotes.Select(p => p.Tick).Append(0).Max();
            if (mappedNotes.Count == 0) tilGroups.Remove(id);
            else if (events.Count > 0 && maxTick.Original > 0) events.RemoveAll(p => p.Tick.Original > maxTick.Original + Time.SingleTick);
        }

        foreach (var (id, notes) in noteGroups.ToList())
        {
            if (notes.Count == 0) noteGroups.Remove(id);
        }
    }

    protected void CreateGroup(int id)
    {
        if (!tilGroups.ContainsKey(id)) tilGroups[id] = [];
        if (!noteGroups.ContainsKey(id)) noteGroups[id] = [];
    }

    protected void SwapGroup(int aId, int bId)
    {
        if (aId == bId) return;
        CreateGroup(aId);
        CreateGroup(bId);

        var aEvents = tilGroups[aId];
        var bEvents = tilGroups[bId];
        tilGroups.Remove(aId);
        tilGroups.Remove(bId);
        foreach (var e in aEvents) e.Timeline = bId;
        foreach (var e in bEvents) e.Timeline = aId;
        tilGroups[aId] = bEvents;
        tilGroups[bId] = aEvents;

        var aNotes = noteGroups[aId];
        var bNotes = noteGroups[bId];
        foreach (var n in aNotes) n.Timeline = bId;
        foreach (var n in bNotes) n.Timeline = aId;

        noteGroups.Remove(aId);
        noteGroups.Remove(bId);
        noteGroups[aId] = bNotes;
        noteGroups[bId] = aNotes;
    }

    protected void FindNoteViolations()
    {
        var violations = new HashSet<mgxc.Note>();
        var noteGroup = mgxc.Notes.Children.GroupBy(n => (n.Tick, n.Lane)).Where(g => g.Count() > 1);

        foreach (var group in noteGroup)
        {
            var notesInGroup = group.ToList();
            for (var i = 0; i < notesInGroup.Count; i++)
            {
                for (var j = i + 1; j < notesInGroup.Count; j++)
                {
                    if (!notesInGroup[i].IsViolate(notesInGroup[j])) continue;
                    violations.Add(notesInGroup[i]);
                    violations.Add(notesInGroup[j]);
                }
            }
        }

        foreach (var note in violations) diag.Report(Severity.Warning, Strings.Diag_note_overlapped_in_different_TIL, note.Tick.Original, note);
    }
}