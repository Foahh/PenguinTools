using mg = PenguinTools.Core.Chart.Models.mgxc;

namespace PenguinTools.Core.Chart.Writer;

public sealed record C2SWriteRequest(string OutPath, mg.Chart Mgxc);
