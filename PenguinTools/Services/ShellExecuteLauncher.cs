using System.Diagnostics;

namespace PenguinTools.Services;

public sealed class ShellExecuteLauncher : IExternalLauncher
{
    public void Launch(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignored
        }
    }
}