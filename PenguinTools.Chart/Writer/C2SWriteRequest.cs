using mg = PenguinTools.Chart.Models.mgxc;

namespace PenguinTools.Chart.Writer;

public sealed record C2SWriteRequest(string OutPath, mg.Chart Mgxc);
