using System.Diagnostics;
using System.Globalization;

namespace PenguinTools.Core.Media;

public sealed class MuaMediaTool(string executablePath) : IMediaTool
{
    private string ExecutablePath { get; } = string.IsNullOrWhiteSpace(executablePath)
        ? throw new ArgumentNullException(nameof(executablePath))
        : executablePath;

    private async Task<ProcessCommandResult> RunAsync(IEnumerable<string> args, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var arg in args) psi.ArgumentList.Add(arg);

        using var proc = new Process { StartInfo = psi };
        proc.Start();

        var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = proc.StandardError.ReadToEndAsync(ct);
        await Task.WhenAll(proc.WaitForExitAsync(ct), stdoutTask, stderrTask);

        return new ProcessCommandResult(psi, proc.ExitCode, await stdoutTask, await stderrTask);
    }

    public async Task<ProcessCommandResult> NormalizeAudioAsync(string src, string dst, decimal offset, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(src);
        ArgumentException.ThrowIfNullOrWhiteSpace(dst);

        var ret = await RunAsync([
            "audio_normalize",
            "-s", src,
            "-d", dst,
            "-o", Math.Round(offset, 6).ToString(CultureInfo.InvariantCulture)
        ], ct);

        ret.ThrowIfFailed();
        return ret;
    }

    public async Task<ProcessCommandResult> CheckAudioValidAsync(string src, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(src);
        return await RunAsync(["audio_check", "-s", src], ct);
    }

    public async Task<ProcessCommandResult> CheckImageValidAsync(string src, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(src);
        return await RunAsync(["image_check", "-s", src], ct);
    }

    public async Task ConvertJacketAsync(string src, string dst, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(src);
        ArgumentException.ThrowIfNullOrWhiteSpace(dst);

        var ret = await RunAsync(["convert_jacket", "-s", src, "-d", dst], ct);
        ret.ThrowIfFailed();
    }

    public async Task ConvertStageAsync(string bg, string stSrc, string stDst, string?[]? fxPaths, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bg);
        ArgumentException.ThrowIfNullOrWhiteSpace(stSrc);
        ArgumentException.ThrowIfNullOrWhiteSpace(stDst);

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
            if (string.IsNullOrWhiteSpace(fxPath)) continue;

            args.Add($"-f{i + 1}");
            args.Add(fxPath);
        }

        var ret = await RunAsync(args, ct);
        ret.ThrowIfFailed();
    }

    public async Task ExtractDdsAsync(string src, string dst, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(src);
        ArgumentException.ThrowIfNullOrWhiteSpace(dst);

        var ret = await RunAsync(["extract_dds", "-s", src, "-d", dst], ct);
        ret.ThrowIfFailed();
    }
}
