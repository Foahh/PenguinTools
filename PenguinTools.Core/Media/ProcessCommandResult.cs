using System.Diagnostics;
using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Media;

public class ProcessCommandResult
{
    internal ProcessCommandResult(ProcessStartInfo startInfo, int exitCode, string stdout, string stderr)
    {
        ExitCode = (InterExitCode)exitCode;
        StandardOutput = stdout.Trim();
        StandardError = stderr.Trim();
        Command = $"{startInfo.FileName} {string.Join(" ", startInfo.ArgumentList)}";
    }

    public InterExitCode ExitCode { get; }
    public string StandardOutput { get; }
    public string StandardError { get; }
    public string Command { get; }

    public bool IsSuccess => ExitCode is InterExitCode.Success or InterExitCode.NoOperation;
    public bool IsNoOperation => ExitCode == InterExitCode.NoOperation;
    public bool IsFailure => !IsSuccess;

    internal void ThrowIfFailed()
    {
        if (!IsFailure) return;

        throw new DiagnosticException(Strings.Error_Command_failed, this);
    }
}
