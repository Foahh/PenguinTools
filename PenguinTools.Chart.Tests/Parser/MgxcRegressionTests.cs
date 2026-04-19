using System.Diagnostics;
using PenguinTools.Chart.Parser;
using Xunit;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Media;

namespace PenguinTools.Chart.Tests.Parser;

public class MgxcRegressionTests
{
    private const string MasterMgxcPath = "/home/fn/Chunithm/Finished/2765/MASTER.mgxc";

    [Fact]
    public async Task Parse_MASTER_mgxc_matches_prior_pipeline()
    {
        if (!File.Exists(MasterMgxcPath)) return;

        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var assetsPath = Path.Combine(repoRoot, "assets.json");
        if (!File.Exists(assetsPath)) return;

        await using var assetsStream = File.OpenRead(assetsPath);
        var assets = new AssetManager(assetsStream);
        var parser = new MgxcParser(new MgxcParseRequest(MasterMgxcPath, assets), new NullMediaTool());

        var result = await parser.ParseAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(MasterMgxcPath, result.Value!.Meta.FilePath);
    }

    private sealed class NullMediaTool : IMediaTool
    {
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
}
