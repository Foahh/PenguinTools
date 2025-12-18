using System.Diagnostics;
using System.Globalization;
using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Media;

public class ProcessCommandResult
{
    internal ProcessCommandResult(Process proc, string stdout, string stderr)
    {
        ExitCode = (InterExitCode)proc.ExitCode;
        StandardOutput = stdout.Trim();
        StandardError = stderr.Trim();
        Command = $"{proc.StartInfo.FileName} {string.Join(" ", proc.StartInfo.ArgumentList)}";
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
        if (!IsFailure) { return; }

        throw new DiagnosticException(Strings.Error_Command_failed, this);
    }
}

public static class Manipulate
{
    private static async Task<ProcessCommandResult> RunAsync(IEnumerable<string> args,
        string bin = "mua",
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = bin,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var arg in args) { psi.ArgumentList.Add(arg); }

        using var proc = new Process();
        proc.StartInfo = psi;

        proc.Start();

        var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = proc.StandardError.ReadToEndAsync(ct);
        await Task.WhenAll(proc.WaitForExitAsync(ct), stdoutTask, stderrTask);

        return new ProcessCommandResult(proc, await stdoutTask, await stderrTask);
    }

    public static async Task<ProcessCommandResult> NormalizeAudioAsync(string src, string dst, decimal offset,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(src)) { throw new ArgumentNullException(nameof(src)); }

        if (string.IsNullOrWhiteSpace(dst)) { throw new ArgumentNullException(nameof(dst)); }

        var ret = await RunAsync([
            "audio_normalize",
            "-s", src,
            "-d", dst,
            "-o", Math.Round(offset, 6).ToString(CultureInfo.InvariantCulture)
        ], ct: ct);

        ret.ThrowIfFailed();
        return ret;
    }

    public static async Task<ProcessCommandResult> CheckAudioValidAsync(string src, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(src)) { throw new ArgumentNullException(nameof(src)); }

        return await RunAsync(["audio_check", "-s", src], ct: ct);
    }

    public static async Task<ProcessCommandResult> CheckImageValidAsync(string src, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(src)) { throw new ArgumentNullException(nameof(src)); }

        return await RunAsync(["image_check", "-s", src], ct: ct);
    }

    public static async Task ConvertJacketAsync(string src, string dst, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(src)) { throw new ArgumentNullException(nameof(src)); }

        if (string.IsNullOrWhiteSpace(dst)) { throw new ArgumentNullException(nameof(dst)); }

        var ret = await RunAsync(["convert_jacket", "-s", src, "-d", dst], ct: ct);
        ret.ThrowIfFailed();
    }

    public static async Task ConvertStageAsync(string bg, string stSrc, string stDst, string?[]? fxPaths,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(bg)) { throw new ArgumentNullException(nameof(bg)); }

        if (string.IsNullOrWhiteSpace(stSrc)) { throw new ArgumentNullException(nameof(stSrc)); }

        if (string.IsNullOrWhiteSpace(stDst)) { throw new ArgumentNullException(nameof(stDst)); }

        var args = new List<string>
        {
            "convert_stage",
            "-b", bg,
            "-s", stSrc,
            "-d", stDst
        };
        for (var i = 0; fxPaths is not null && i < fxPaths.Length && i < 4; i++)
        {
            var fxPath = fxPaths[i];
            if (string.IsNullOrWhiteSpace(fxPath)) { continue; }

            args.Add($"-f{i + 1}");
            args.Add(fxPath);
        }

        var ret = await RunAsync(args, ct: ct);
        ret.ThrowIfFailed();
    }

    public static async Task ExtractDdsAsync(string src, string dst, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(src)) { throw new ArgumentNullException(nameof(src)); }

        if (string.IsNullOrWhiteSpace(dst)) { throw new ArgumentNullException(nameof(dst)); }

        var ret = await RunAsync(["extract_dds", "-s", src, "-d", dst], ct: ct);
        ret.ThrowIfFailed();
    }
}