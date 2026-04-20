namespace PenguinTools.Workflow;

public sealed record ExportOutputPaths(
    string AudioFolder,
    string StageFolder,
    string CueFileFolder,
    string EventFolder,
    string ReleaseTagPath)
{
    /// <summary>Resolves the on-disk bundle root from a working directory and stable option identifier.</summary>
    public static string ResolveBundleRootPath(string workingDirectory, string optionId)
    {
        var normalized = Path.TrimEndingDirectorySeparator(workingDirectory);
        var folder = Path.GetFileName(normalized);
        return folder == optionId ? workingDirectory : Path.Combine(workingDirectory, optionId);
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
