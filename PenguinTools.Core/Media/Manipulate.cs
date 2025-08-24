using PenguinTools.Core.Resources;
using System.Diagnostics;
using System.Globalization;

namespace PenguinTools.Core.Media;

public enum ExitCode
{
    Success = 0,
    Failure = 1,
    NoOperation = 2
}

public class ProcessCommandResult
{
    internal ProcessCommandResult(Process proc, string stdout, string stderr)
    {
        ExitCode = (ExitCode)proc.ExitCode;
        StandardOutput = stdout.Trim();
        StandardError = stderr.Trim();
        Command = $"{proc.StartInfo.FileName} {string.Join(" ", proc.StartInfo.ArgumentList)}";
    }

    public ExitCode ExitCode { get; }
    public string StandardOutput { get; }
    public string StandardError { get; }
    public string Command { get; }

    public bool IsSuccess => ExitCode is ExitCode.Success or ExitCode.NoOperation;
    public bool IsNoOperation => ExitCode == ExitCode.NoOperation;
    public bool IsFailure => !IsSuccess;

    internal void ThrowIfFailed()
    {
        if (!IsFailure) return;
        throw new DiagnosticException(Strings.Error_Command_failed, this);
    }
}

public static class Manipulate
{
    private static async Task<ProcessCommandResult> RunAsync(IEnumerable<string> args, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "mua",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var arg in args) psi.ArgumentList.Add(arg);

        using var proc = new Process();
        proc.StartInfo = psi;

        proc.Start();

        var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = proc.StandardError.ReadToEndAsync(ct);
        await Task.WhenAll(proc.WaitForExitAsync(ct), stdoutTask, stderrTask);

        return new ProcessCommandResult(proc, await stdoutTask, await stderrTask);
    }

    public static async Task<ProcessCommandResult> NormalizeAsync(string src, string dst, decimal offset, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(src)) throw new ArgumentNullException(nameof(src));
        if (string.IsNullOrWhiteSpace(dst)) throw new ArgumentNullException(nameof(dst));

        var ret = await RunAsync([
            "an",
            "-s", src,
            "-d", dst,
            "-o", Math.Round(offset, 6).ToString(CultureInfo.InvariantCulture),
        ], ct);

        ret.ThrowIfFailed();
        return ret;
    }

    public static async Task<ProcessCommandResult> IsAudioValidAsync(string src, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(src)) throw new ArgumentNullException(nameof(src));
        return await RunAsync(["ai", "-s", src], ct);
    }

    public static async Task<ProcessCommandResult> IsImageValidAsync(string src, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(src)) throw new ArgumentNullException(nameof(src));
        return await RunAsync(["ii", "-s", src], ct);
    }

    public static async Task ConvertJacketAsync(string src, string dst, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(src)) throw new ArgumentNullException(nameof(src));
        if (string.IsNullOrWhiteSpace(dst)) throw new ArgumentNullException(nameof(dst));

        var ret = await RunAsync(["cj", "-s", src, "-d", dst], ct);
        ret.ThrowIfFailed();
    }

    public static async Task ConvertStageAsync(string bg, string stSrc, string stDst, string?[]? fxPaths, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bg)) throw new ArgumentNullException(nameof(bg));
        if (string.IsNullOrWhiteSpace(stSrc)) throw new ArgumentNullException(nameof(stSrc));
        if (string.IsNullOrWhiteSpace(stDst)) throw new ArgumentNullException(nameof(stDst));

        var args = new List<string>
        {
            "cs",
            "-b",
            bg,
            "-s",
            stSrc,
            "-d",
            stDst
        };
        for (var i = 0; fxPaths is not null && i < fxPaths.Length && i < 4; i++)
        {
            var fxPath = fxPaths[i];
            if (string.IsNullOrWhiteSpace(fxPath)) continue;
            args.Add($"-f{i + 1}");
            args.Add(fxPath);
        }

        var ret = await RunAsync(args, ct);
        ret.ThrowIfFailed();
    }

    public static async Task ExtractDdsAsync(string src, string dst, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(src)) throw new ArgumentNullException(nameof(src));
        if (string.IsNullOrWhiteSpace(dst)) throw new ArgumentNullException(nameof(dst));

        var ret = await RunAsync(["ed", "-s", src, "-d", dst], ct);
        ret.ThrowIfFailed();
    }
}