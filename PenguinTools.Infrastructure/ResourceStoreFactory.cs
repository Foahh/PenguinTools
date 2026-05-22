using PenguinTools.Core;

namespace PenguinTools.Infrastructure;

public static class ResourceStoreFactory
{
    public static IResourceStore Create(
        ResourceStoreOptions options,
        string tempWorkPath,
        string sharedCachePath)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempWorkPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sharedCachePath);

        return options.Mode switch
        {
            ResourceStoreMode.Embedded => new EmbeddedResourceStore(
                options.EmbeddedAssembly,
                tempWorkPath,
                sharedCachePath),
            ResourceStoreMode.External => new FileResourceStore(
                ResolveExternalAssetsDirectory(options),
                tempWorkPath),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.Mode, null)
        };
    }

    public static string ResolveInfrastructureAssetsPath(ResourceStoreOptions options, IApplicationPaths paths)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(paths);

        return options.Mode switch
        {
            ResourceStoreMode.External => ResolveExternalAssetsDirectory(options),
            ResourceStoreMode.Embedded => paths.SharedAssetCachePath,
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.Mode, null)
        };
    }

    private static string ResolveExternalAssetsDirectory(ResourceStoreOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ExternalAssetsDirectory))
            return options.ExternalAssetsDirectory;

        return Path.Combine(AppContext.BaseDirectory, ResourceStoreOptions.DefaultExternalAssetsSubdirectory);
    }
}
