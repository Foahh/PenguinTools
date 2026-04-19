namespace PenguinTools.Workflow;

public sealed record ExportOutputPaths(
    string AudioFolder,
    string StageFolder,
    string CueFileFolder,
    string EventFolder,
    string ReleaseTagPath)
{
    /// <summary>Resolves the on-disk bundle root from a working directory and four-letter option name.</summary>
    public static string ResolveBundleRootPath(string workingDirectory, string optionName)
    {
        var normalized = Path.TrimEndingDirectorySeparator(workingDirectory);
        var folder = Path.GetFileName(normalized);
        return folder == optionName ? workingDirectory : Path.Combine(workingDirectory, optionName);
    }

    public static ExportOutputPaths FromOptionDirectory(string rootPath)
    {
        return new ExportOutputPaths(
            Path.Combine(rootPath, "audio"),
            Path.Combine(rootPath, "stage"),
            Path.Combine(rootPath, "cueFile"),
            Path.Combine(rootPath, "event"),
            Path.Combine(rootPath, "releaseTag"));
    }
}