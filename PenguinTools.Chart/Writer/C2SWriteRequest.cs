using umgr = PenguinTools.Chart.Models.umgr;

namespace PenguinTools.Chart.Writer;

public sealed record C2SWriteRequest(string OutPath, umgr.Chart Mgxc);
