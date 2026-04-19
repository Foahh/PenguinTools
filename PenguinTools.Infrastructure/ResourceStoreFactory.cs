using System.Reflection;
using PenguinTools.Core;

namespace PenguinTools.Infrastructure;

public static class ResourceStoreFactory
{
    public static IResourceStore Create(Assembly assembly, string? baseDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var appBaseDirectory = string.IsNullOrWhiteSpace(baseDirectory) ? AppContext.BaseDirectory : baseDirectory;
        var assetDirectory = Path.Combine(appBaseDirectory, "assets");

        if (File.Exists(Path.Combine(assetDirectory, "assets.json")))
        {
            return new FileResourceStore(assetDirectory);
        }

        return new EmbeddedResourceStore(assembly);
    }
}
