using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Infrastructure;
using Xunit;

namespace PenguinTools.Tests.Infrastructure;

public class ExecutionInfoProviderTests
{
    [Fact]
    public void Create_UsesExternalAssetsDirectory_WhenModeIsExternal()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var userData = Path.Combine(tempRoot, "userdata");
        var sharedCache = Path.Combine(tempRoot, "cache");
        var externalAssets = Path.Combine(tempRoot, "assets");
        Directory.CreateDirectory(externalAssets);

        try
        {
            var paths = new TestApplicationPaths(
                Path.Combine(tempRoot, "temp"),
                userData,
                sharedCache);
            var options = ResourceStoreOptions.External(externalAssets);

            var info = ExecutionInfoProvider.Create(paths, options);

            Assert.Equal(externalAssets, info.InfrastructureAssetsPath);
            Assert.Equal("External", info.AssetsMode);
            Assert.Equal(Path.Combine(userData, AssetManager.PlusAssetsFileName), info.PlusAssetsPath);
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void Create_UsesSharedCache_WhenModeIsEmbedded()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var userData = Path.Combine(tempRoot, "userdata");
        var sharedCache = Path.Combine(tempRoot, "cache");

        try
        {
            var paths = new TestApplicationPaths(
                Path.Combine(tempRoot, "temp"),
                userData,
                sharedCache);
            var options = ResourceStoreOptions.Embedded();

            var info = ExecutionInfoProvider.Create(paths, options);

            Assert.Equal(sharedCache, info.InfrastructureAssetsPath);
            Assert.Equal("Embedded", info.AssetsMode);
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }

    private sealed class TestApplicationPaths(string tempWorkPath, string userDataPath, string sharedAssetCachePath)
        : IApplicationPaths
    {
        public string TempWorkPath { get; } = tempWorkPath;
        public string UserDataPath { get; } = userDataPath;
        public string SharedAssetCachePath { get; } = sharedAssetCachePath;
    }
}
