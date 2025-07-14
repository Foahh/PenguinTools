using System.ComponentModel;

namespace PenguinTools.Core.Chart.Models;

public enum AirDirection
{
    [Description("Up")] IR,
    [Description("Up Left")] Ul,
    [Description("Up Right")] Ur,
    [Description("Down")] Dw,
    [Description("Down Left")] Dl,
    [Description("Down Right")] Dr
}

public enum Color
{
    [Description("Default")] DEF,
    [Description("None")] Non,
    [Description("Pink")] Pnk,
    [Description("Green")] GRN,
    [Description("Lime")] Lim,
    [Description("Red")] Red,
    [Description("Black")] Blk,
    [Description("Violet")] Vlt,
    [Description("Blue")] BLU,
    [Description("Dodger Blue")] Dgr,
    [Description("Aqua")] Aqa,
    [Description("Cyan")] Cyn,
    [Description("Yellow")] Yel,
    [Description("Orange")] Orn,
    [Description("Gray")] Gry,
    [Description("Purple")] Ppl
}

public enum ExEffect
{
    [Description("Up")] Up,
    [Description("Down")] Dw,
    [Description("Center")] CE,
    [Description("Left")] Lc,
    [Description("Right")] Rc,
    [Description("Rotate Left")] Ls,
    [Description("Rotate Right")] Rs,
    [Description("InOut")] Bs
}

public enum Joint
{
    [Description("Control")] C,
    [Description("Step")] D
}