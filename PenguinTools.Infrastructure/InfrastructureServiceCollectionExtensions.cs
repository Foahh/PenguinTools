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
        services.AddSingleton<IMediaTool>(provider =>
        {
            var resources = provider.GetRequiredService<IEmbeddedResourceStore>();

            if (resources.HasResource("mua.exe"))
            {
                var toolPath = resources.ExtractToTemp("mua.exe");
                return new MuaMediaTool(toolPath);
            }

            if (resources.HasResource("mua"))
            {
                var toolPath = resources.ExtractToTemp("mua");
                return new MuaMediaTool(toolPath);
            }

            return new MuaMediaTool("mua");
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
