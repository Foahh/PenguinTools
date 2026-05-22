using System.Diagnostics;

namespace PenguinTools.Infrastructure;

public static class ShellExplorer
{
    public static void OpenDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(path);

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = path,
            UseShellExecute = true
        });
    }
}
