using System.Diagnostics;
using PenguinTools.Chart.Parser.mgxc;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Media;
using Xunit;

namespace PenguinTools.Tests.Parser;

public class MgxcRegressionTests
{
    [Fact]
    public async Task ParseKnownSample_StillProducesChart()
    {
        var masterMgxcPath = Path.Combine(ChartTestPaths.AssetsDirectory, "Ver seX.mgxc");
        if (!File.Exists(masterMgxcPath))
            return;

        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var assetsPath = Path.Combine(repoRoot, "assets.json");
        if (!File.Exists(assetsPath))
            return;

        await using var assetsStream = File.OpenRead(assetsPath);
        var userDir = Path.Combine(Path.GetTempPath(), "PenguinChartTests", "user-assets");
        Directory.CreateDirectory(userDir);
        var assets = new AssetManager(assetsStream, userDir);
        var parser = new MgxcParser(new MgxcParseRequest(masterMgxcPath, assets), new NullMediaTool());

        var result = await parser.ParseAsync(TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value!.Notes.Children);
    }

    private sealed class NullMediaTool : IMediaTool
    {
        public Task<ProcessCommandResult> NormalizeAudioAsync(string src, string dst, decimal offset,
            CancellationToken ct = default)
        {
            return Task.FromResult(Ok());
        }

        public Task<ProcessCommandResult> CheckAudioValidAsync(string src, CancellationToken ct = default)
        {
            return Task.FromResult(Ok());
        }

        public Task<ProcessCommandResult> CheckImageValidAsync(string src, CancellationToken ct = default)
        {
            return Task.FromResult(Ok());
        }

        public Task ConvertJacketAsync(string src, string dst, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task ConvertStageAsync(string bg, string stSrc, string stDst, string?[]? fxPaths,
            CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task ExtractDdsAsync(string src, string dst, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        private static ProcessCommandResult Ok()
        {
            return new ProcessCommandResult(new ProcessStartInfo { FileName = "null" }, (int)InterExitCode.Success, "",
                "");
        }
    }
}
