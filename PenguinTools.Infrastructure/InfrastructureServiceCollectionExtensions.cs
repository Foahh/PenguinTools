using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Media;

namespace PenguinTools.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPenguinInfrastructure(this IServiceCollection services, Assembly resourceAssembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(resourceAssembly);

        services.AddSingleton<IEmbeddedResourceStore>(_ => new EmbeddedResourceStore(resourceAssembly));
        services.AddSingleton<IInfrastructureAssetProvider, InfrastructureAssetProvider>();
        services.AddSingleton<IMediaTool>(provider =>
        {
            var assets = provider.GetRequiredService<IInfrastructureAssetProvider>();
            return new MuaMediaTool(assets.GetPath(InfrastructureAsset.MuaExecutable));
        });
        services.AddSingleton<AssetManager>(provider =>
        {
            var resources = provider.GetRequiredService<IEmbeddedResourceStore>();
            using var stream = resources.OpenRead("assets.json");
            return new AssetManager(stream);
        });

        return services;
    }
}
