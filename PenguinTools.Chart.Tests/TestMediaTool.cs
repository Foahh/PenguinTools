using System.Diagnostics;
using PenguinTools.Core;
using PenguinTools.Media;

namespace PenguinTools.Chart.Tests;

internal sealed class TestMediaTool : IMediaTool
{
    public static readonly TestMediaTool Instance = new();

    private static ProcessCommandResult Ok() =>
        new(new ProcessStartInfo { FileName = "null" }, (int)InterExitCode.Success, "", "");

    public Task<ProcessCommandResult> NormalizeAudioAsync(string src, string dst, decimal offset, CancellationToken ct = default) =>
        Task.FromResult(Ok());

    public Task<ProcessCommandResult> CheckAudioValidAsync(string src, CancellationToken ct = default) =>
        Task.FromResult(Ok());

    public Task<ProcessCommandResult> CheckImageValidAsync(string src, CancellationToken ct = default) =>
        Task.FromResult(Ok());

    public Task ConvertJacketAsync(string src, string dst, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task ConvertStageAsync(string bg, string stSrc, string stDst, string?[]? fxPaths, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task ExtractDdsAsync(string src, string dst, CancellationToken ct = default) =>
        Task.CompletedTask;
}
