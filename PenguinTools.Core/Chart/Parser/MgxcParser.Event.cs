using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Chart.Parser;

using mg = Models.mgxc;

public partial class MgxcParser
{
    private void ParseEvent(BinaryReader br)
    {
        var name = br.ReadUtf8String(4);
        mg.Event? e = null;

        if (name == "beat")
        {
            e = new mg.BeatEvent
            {
                Bar = (int)br.ReadField(),
                Numerator = (int)br.ReadField(),
                Denominator = (int)br.ReadField()
            };
        }
        else if (name == "bpm ")
        {
            e = new mg.BpmEvent
            {
                Tick = (int)br.ReadField(),
                Bpm = br.ReadField().Round()
            };
        }
        else if (name == "smod")
        {
            e = new mg.NoteSpeedEvent
            {
                Tick = (int)br.ReadField(),
                Speed = br.ReadField().Round()
            };
        }
        else if (name == "til ")
        {
            e = new mg.ScrollSpeedEvent
            {
                Timeline = (int)br.ReadField(),
                Tick = (int)br.ReadField(),
                Speed = br.ReadField().Round()
            };
        }
        else if (name == "bmrk")
        {
            br.ReadWideField(); // hash
            e = new mg.BookmarkEvent
            {
                Tick = (int)br.ReadField(),
                Tag = (string)br.ReadWideField()
            };
            br.ReadWideField(); // rgb
        }
        else if (name == "mbkm")
        {
            e = new mg.BreakingMarker
            {
                Tick = (int)br.ReadField()
            };
        }
        else if (name == "rimg")
        {
            br.ReadField();
            br.ReadField();
            br.ReadWideField();
            br.ReadInt32();
            return;
        }

        if (e == null)
        {
            // avoid misalignment
            var msg = string.Format(Strings.MgCrit_Unrecognized_event, name, br.BaseStream.Position);
            throw new DiagnosticException(msg, Mgxc);
        }
        Mgxc.Events.AppendChild(e);

        br.ReadInt32(); // 00 00 00 00
    }
}