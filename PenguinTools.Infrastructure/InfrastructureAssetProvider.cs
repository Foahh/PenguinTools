using System.Collections.Concurrent;
using PenguinTools.Assets;
using PenguinTools.Core;

namespace PenguinTools.Infrastructure;

public interface IInfrastructureAssetProvider
{
    string GetPath(InfrastructureAsset asset);
}

public sealed class InfrastructureAssetProvider(IResourceStore resources) : IInfrastructureAssetProvider
{
    private readonly ConcurrentDictionary<InfrastructureAsset, string> _paths = new();

    private IResourceStore Resources { get; } = resources ?? throw new ArgumentNullException(nameof(resources));

    public string GetPath(InfrastructureAsset asset)
    {
        return _paths.GetOrAdd(asset, ResolvePath);
    }

    private string ResolvePath(InfrastructureAsset asset)
    {
        if (asset == InfrastructureAsset.MuaExecutable) return ResolveMuaExecutablePath();

        var resourceName = InfrastructureResourceNames.GetResourceName(asset);
        return Resources.ExtractToTemp(resourceName);
    }

    private string ResolveMuaExecutablePath()
    {
        if (OperatingSystem.IsWindows() && Resources.HasResource(InfrastructureResourceNames.MuaExecutableWindows))
            return Resources.ExtractToTemp(InfrastructureResourceNames.MuaExecutableWindows);

        if (OperatingSystem.IsLinux() && Resources.HasResource(InfrastructureResourceNames.MuaExecutableUnix))
            return Resources.ExtractToTemp(InfrastructureResourceNames.MuaExecutableUnix);

        if (Resources.HasResource(InfrastructureResourceNames.MuaExecutableWindows))
            return Resources.ExtractToTemp(InfrastructureResourceNames.MuaExecutableWindows);

        if (Resources.HasResource(InfrastructureResourceNames.MuaExecutableUnix))
            return Resources.ExtractToTemp(InfrastructureResourceNames.MuaExecutableUnix);

        return OperatingSystem.IsWindows()
            ? InfrastructureResourceNames.MuaExecutableWindows
            : InfrastructureResourceNames.MuaExecutableUnix;
    }
}
