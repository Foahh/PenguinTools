using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Infrastructure;
using PenguinTools.Media;

namespace PenguinTools.CLI;

internal sealed class CliRuntime(IResourceStore resourceStore, AssetManager assets, IMediaTool mediaTool, IInfrastructureAssetProvider assetProvider) : IDisposable
{
    public IResourceStore ResourceStore { get; } = resourceStore;
    public AssetManager Assets { get; } = assets;
    public IMediaTool MediaTool { get; } = mediaTool;
    public IInfrastructureAssetProvider AssetProvider { get; } = assetProvider;

    public static CliRuntime Create()
    {
#pragma warning disable CA2000
        var resourceStore = ResourceStoreFactory.Create(typeof(InfrastructureAssetProvider).Assembly);
#pragma warning restore CA2000

        try
        {
            var assetProvider = new InfrastructureAssetProvider(resourceStore);
            var assets = new AssetManager(resourceStore.OpenRead("assets.json"));
            var mediaTool = new MuaMediaTool(assetProvider.GetPath(InfrastructureAsset.MuaExecutable));
            return new CliRuntime(resourceStore, assets, mediaTool, assetProvider);
        }
        catch
        {
            resourceStore.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        ResourceStore.Dispose();
    }
}
