namespace PenguinTools.Chart.Tests.Parser;

/// <summary>
/// Paired MASTER.ugc / MASTER.mgxc under /home/fn/Chunithm/Finished.
/// <see cref="ParityVerifiedFolders"/> lists charts where UGC vs MGXC Summarize() matches on this checkout; expand as the parser improves.
/// </summary>
public static class FinishedChartSampleCases
{
    public static IEnumerable<object[]> MasterPairs()
    {
        const string root = "/home/fn/Chunithm/Finished";
        if (!Directory.Exists(root)) yield break;
        foreach (var dir in Directory.GetDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (!ParityVerifiedFolders.Contains(name)) continue;
            var ugc = Path.Combine(dir, "MASTER.ugc");
            var mgxc = Path.Combine(dir, "MASTER.mgxc");
            if (File.Exists(ugc) && File.Exists(mgxc))
                yield return new object[] { name, ugc, mgxc };
        }
    }

    private static readonly HashSet<string> ParityVerifiedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Unsinkable Memory",
        "Sudden Visitor",
        "Oracle",
        "Kannagara",
        "HEADROOM WITH PLEASANT RHYTHM",
    };
}
