using PenguinTools.Core.Chart.Models;
using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Chart.Parser;

using mgxc = Models.mgxc;

internal enum NoteType : sbyte
{
    Unknown = 0x00,
    Tap = 0x01,
    ExTap = 0x02,
    Flick = 0x03,
    Damage = 0x04,
    Hold = 0x05,
    Slide = 0x06,
    Air = 0x07,
    AirHold = 0x08,
    AirSlide = 0x09,
    AirCrush = 0x0A,
    Click = 0x0B,
    Last = 0x0D
}

internal enum LongAttr : sbyte
{
    None = 0x00,
    Begin = 0x01,
    Step = 0x02,
    Control = 0x03,
    CurveControl = 0x04,
    End = 0x05,
    EndNoAct = 0x06
}

internal enum Direction : sbyte
{
    None = 0x00,
    Auto = 0x01,
    Up = 0x02,
    Down = 0x03,
    Center = 0x04,
    Left = 0x05,
    Right = 0x06,
    UpLeft = 0x07,
    UpRight = 0x08,
    DownLeft = 0x09,
    DownRight = 0x0A,
    RotateLeft = 0x0B,
    RotateRight = 0x0C,
    InOut = 0x0D,
    OutIn = 0x0E
}

internal enum ExAttr : sbyte
{
    None = 0x00,
    Invert = 0x01,
    HasNote = 0x02,
    ExJdg = 0x03
}

public partial class MgxcParser
{
    private mgxc.Note? lastNote;
    private mgxc.Note? lastParentNote;

    private void ParseNote(BinaryReader br)
    {
        var type = (NoteType)br.ReadSByte();
        var longAttr = (LongAttr)br.ReadSByte();
        var direction = (Direction)br.ReadSByte();
        var exAttr = (ExAttr)br.ReadSByte();
        var variationId = br.ReadSByte();
        var x = br.ReadSByte();
        var width = br.ReadInt16();
        var height = br.ReadInt32();
        var tick = br.ReadInt32();
        var timelineId = br.ReadInt32();
        var optionValue = type == NoteType.AirCrush && longAttr == LongAttr.Begin ? br.ReadInt32() : 0;

        mgxc.Note? note = null;
        var isChildNote = false;
        var isPairNote = false;

        if (type == NoteType.Tap)
        {
            note = new mgxc.Tap();
        }
        else if (type == NoteType.ExTap)
        {
            var exNote = new mgxc.ExTap();
            exNote.Effect = direction switch
            {
                Direction.Up => ExEffect.Up,
                Direction.Down => ExEffect.Dw,
                Direction.Center => ExEffect.CE,
                Direction.Left => ExEffect.Ls,
                Direction.Right => ExEffect.Rs,
                Direction.RotateLeft => ExEffect.Lc,
                Direction.RotateRight => ExEffect.Rc,
                Direction.InOut => ExEffect.Bs,
                Direction.OutIn => ExEffect.CE,
                _ => ExEffect.Up
            };
            note = exNote;
        }
        else if (type == NoteType.Flick)
        {
            note = new mgxc.Flick();
        }
        else if (type == NoteType.Damage)
        {
            note = new mgxc.Damage();
        }
        else if (type == NoteType.Hold)
        {
            if (longAttr == LongAttr.Begin)
            {
                note = new mgxc.Hold();
            }
            else if (longAttr == LongAttr.End)
            {
                note = new mgxc.HoldJoint();
                isChildNote = true;
            }
            else
            {
                var msg = string.Format(Strings.Diag_Invalid_joint_type_note, typeof(mgxc.HoldJoint));
                diag.Report(Severity.Warning, msg, tick, longAttr);
            }
        }
        else if (type == NoteType.Slide)
        {
            if (longAttr == LongAttr.Begin)
            {
                note = new mgxc.Slide();
            }
            else
            {
                var exNote = new mgxc.SlideJoint();
                if (longAttr is LongAttr.Step or LongAttr.End)
                {
                    exNote.Joint = Joint.D;
                }
                else if (longAttr is LongAttr.Control or LongAttr.EndNoAct or LongAttr.CurveControl)
                {
                    exNote.Joint = Joint.C;
                }
                else
                {
                    var msg = string.Format(Strings.Diag_Invalid_joint_type_note, typeof(mgxc.SlideJoint));
                    diag.Report(Severity.Warning, msg, tick, longAttr);
                }
                note = exNote;
                isChildNote = true;
            }
        }
        else if (type == NoteType.Air)
        {
            var exNote = new mgxc.Air();
            switch (direction)
            {
                case Direction.Up: exNote.Direction = AirDirection.IR; break;
                case Direction.Down: exNote.Direction = AirDirection.Dw; break;
                case Direction.UpLeft: exNote.Direction = AirDirection.Ul; break;
                case Direction.UpRight: exNote.Direction = AirDirection.Ur; break;
                case Direction.DownLeft: exNote.Direction = AirDirection.Dl; break;
                case Direction.DownRight: exNote.Direction = AirDirection.Dr; break;
                default: exNote.Direction = AirDirection.IR; break;
            }
            exNote.Color = exAttr == ExAttr.Invert ? Color.Pnk : Color.DEF;
            note = exNote;
            isPairNote = true;
        }
        else if (type is NoteType.AirHold or NoteType.AirSlide)
        {
            if (longAttr == LongAttr.Begin)
            {
                var exNote = new mgxc.AirSlide();
                if (lastNote is mgxc.Air oldLastNote)
                {
                    lastNote.Parent?.RemoveChild(lastNote);
                    lastNote = oldLastNote.PairNote;
                    exNote.Color = oldLastNote.Color;
                }

                exNote.Height = height;
                note = exNote;
                isPairNote = true;
            }
            else
            {
                var exNote = new mgxc.AirSlideJoint();
                if (longAttr is LongAttr.Step or LongAttr.End)
                {
                    exNote.Joint = Joint.D;
                }
                else if (longAttr is LongAttr.Control or LongAttr.EndNoAct or LongAttr.CurveControl)
                {
                    exNote.Joint = Joint.C;
                }
                else
                {
                    var msg = string.Format(Strings.Diag_Invalid_joint_type_note, typeof(mgxc.AirSlideJoint));
                    diag.Report(Severity.Warning, msg, tick, longAttr);
                }
                exNote.Height = height;
                note = exNote;
                isChildNote = true;
            }
        }
        else if (type == NoteType.AirCrush)
        {
            var color = variationId switch
            {
                0 => Color.DEF,
                1 => Color.Red, // Red
                2 => Color.Orn, // Orange
                3 => Color.Yel, // Yellow
                4 => Color.GRN, // Green
                5 => Color.Aqa, // Sky
                6 => Color.BLU, // Blue
                7 => Color.Ppl, // Violet
                8 => Color.Vlt, // Pink
                9 => Color.Ppl, // Violet
                10 => Color.Gry, // White
                11 => Color.Blk, // Black
                12 => Color.Lim, // Grass
                13 => Color.Cyn, // Sky Blue
                14 => Color.Dgr, // Cobalt Blue
                15 => Color.Pnk, // Purple
                35 => Color.Non, // Transparent
                _ => Color.DEF
            };

            if (longAttr == LongAttr.Begin)
            {
                var exNote = new mgxc.AirCrash();
                exNote.Color = color;
                exNote.Height = height;
                exNote.Density = optionValue;
                note = exNote;
            }
            else
            {
                var exNote = new mgxc.AirCrashJoint();
                if (longAttr is LongAttr.Step)
                {
                    var msg = string.Format(Strings.Diag_Invalid_joint_type_note, typeof(mgxc.AirCrashJoint));
                    diag.Report(Severity.Warning, msg, tick, longAttr);
                }
                exNote.Height = height;
                note = exNote;
                isChildNote = true;
            }
        }

        if (note == null)
        {
            var msg = string.Format(Strings.Error_Unrecognized_note_type, (int)type, br.BaseStream.Position);
            diag.Report(Severity.Warning, msg, tick, type);
            return;
        }

        note.Tick = tick;
        note.Lane = x;
        note.Width = width;
        note.Timeline = timelineId;

        if (isChildNote) lastParentNote?.AppendChild(note);
        else mgxc.Notes.AppendChild(note);

        if (isPairNote)
        {
            switch (lastNote)
            {
                case mgxc.PositiveNote lastP when note is mgxc.NegativeNote newN:
                    lastP.MakePair(newN);
                    break;
                case mgxc.NegativeNote lastN when note is mgxc.PositiveNote newP:
                    lastN.MakePair(newP);
                    break;
                default:
                    throw new DiagnosticException(Strings.Error_pairing_notes_incompatible, new[] { note, lastNote }, note.Tick.Original);
            }
        }

        if (!isChildNote) lastParentNote = note;

        lastNote = note;
    }
}