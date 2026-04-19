namespace PenguinTools.Chart.Tests;

/// <summary>
///     Paths under the test project (no machine-specific roots).
/// </summary>
internal static class ChartTestPaths
{
    /// <summary>
    ///     <c>PenguinTools.Chart.Tests/Assets</c> — paired <c>.ugc</c> / <c>.mgxc</c> samples live here.
    /// </summary>
    public static string AssetsDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets"));
}