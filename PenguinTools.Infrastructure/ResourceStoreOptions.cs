using System.Reflection;
using PenguinTools.Assets;

namespace PenguinTools.Infrastructure;

public sealed class ResourceStoreOptions
{
    public const string ModeEnvironmentVariable = "PENGUIN_TOOLS_ASSETS_MODE";
    public const string PathEnvironmentVariable = "PENGUIN_TOOLS_ASSETS_PATH";
    public const string DefaultExternalAssetsSubdirectory = "assets";

    public ResourceStoreMode Mode { get; init; } = ResourceStoreMode.Embedded;

    public Assembly EmbeddedAssembly { get; init; } = typeof(InfrastructureAssets).Assembly;

    public string? ExternalAssetsDirectory { get; init; }

    public static ResourceStoreOptions Embedded()
    {
        return new ResourceStoreOptions { Mode = ResourceStoreMode.Embedded };
    }

    public static ResourceStoreOptions External(string? assetsDirectory = null)
    {
        return new ResourceStoreOptions
        {
            Mode = ResourceStoreMode.External,
            ExternalAssetsDirectory = assetsDirectory
        };
    }

    public static ResourceStoreOptions Resolve()
    {
        var mode = ResolveMode();
        var externalDirectory = ResolveExternalAssetsDirectory();
        return new ResourceStoreOptions
        {
            Mode = mode,
            EmbeddedAssembly = typeof(InfrastructureAssets).Assembly,
            ExternalAssetsDirectory = externalDirectory
        };
    }

    private static ResourceStoreMode ResolveMode()
    {
        var fromEnv = Environment.GetEnvironmentVariable(ModeEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(fromEnv)) return ResourceStoreMode.Embedded;

        return fromEnv.Trim().Equals("external", StringComparison.OrdinalIgnoreCase)
            ? ResourceStoreMode.External
            : ResourceStoreMode.Embedded;
    }

    private static string? ResolveExternalAssetsDirectory()
    {
        var fromEnv = Environment.GetEnvironmentVariable(PathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnv)) return Path.GetFullPath(fromEnv.Trim());

        return null;
    }
}