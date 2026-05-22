using Microsoft.Extensions.DependencyInjection;
using PenguinTools.Assets;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Media;

namespace PenguinTools.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPenguinInfrastructure(
        this IServiceCollection services,
        ResourceStoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var resolvedOptions = options ?? ResourceStoreOptions.Resolve();

        services.AddSingleton<IApplicationPaths>(_ => ApplicationPaths.Create());
        services.AddSingleton(resolvedOptions);
        services.AddSingleton<IResourceStore>(sp =>
        {
            var paths = sp.GetRequiredService<IApplicationPaths>();
            var storeOptions = sp.GetRequiredService<ResourceStoreOptions>();
            return ResourceStoreFactory.Create(storeOptions, paths.TempWorkPath, paths.SharedAssetCachePath);
        });
        services.AddSingleton<IInfrastructureAssetProvider, InfrastructureAssetProvider>();
        services.AddSingleton<IMediaTool>(provider =>
        {
            var assets = provider.GetRequiredService<IInfrastructureAssetProvider>();
            return new MuaMediaTool(assets.GetPath(InfrastructureAsset.MuaExecutable));
        });
        services.AddSingleton<AssetManager>(provider =>
        {
            var resources = provider.GetRequiredService<IResourceStore>();
            var paths = provider.GetRequiredService<IApplicationPaths>();
            using var stream = resources.OpenRead(InfrastructureResourceNames.AssetsJson);
            return new AssetManager(stream, paths.UserDataPath);
        });

        return services;
    }
}
