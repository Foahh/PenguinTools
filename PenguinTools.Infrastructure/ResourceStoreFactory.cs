using System.Reflection;
using PenguinTools.Core;

namespace PenguinTools.Infrastructure;

public static class ResourceStoreFactory
{
    public static IResourceStore Create(Assembly assembly, string tempWorkPath, string? baseDirectory = null,
        string? sharedCachePath = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempWorkPath);

        var appBaseDirectory = string.IsNullOrWhiteSpace(baseDirectory) ? AppContext.BaseDirectory : baseDirectory;
        var assetDirectory = Path.Combine(appBaseDirectory, "assets");

        if (File.Exists(Path.Combine(assetDirectory, "assets.json")))
            return new FileResourceStore(assetDirectory, tempWorkPath);

        return new EmbeddedResourceStore(assembly, tempWorkPath, sharedCachePath);
    }
}