using System.Collections.Concurrent;
using PenguinTools.Core;

namespace PenguinTools.Infrastructure;

public enum InfrastructureAsset
{
    MuaExecutable,
    StageTemplate,
    NotesFieldTemplate,
    DummyAcb
}

public interface IInfrastructureAssetProvider
{
    string GetPath(InfrastructureAsset asset);
}

public sealed class InfrastructureAssetProvider(IEmbeddedResourceStore resources) : IInfrastructureAssetProvider
{
    private readonly ConcurrentDictionary<InfrastructureAsset, string> _paths = new();

    private IEmbeddedResourceStore Resources { get; } = resources ?? throw new ArgumentNullException(nameof(resources));

    public string GetPath(InfrastructureAsset asset)
    {
        return _paths.GetOrAdd(asset, ResolvePath);
    }

    private string ResolvePath(InfrastructureAsset asset)
    {
        return asset switch
        {
            InfrastructureAsset.MuaExecutable => ResolveMuaExecutablePath(),
            InfrastructureAsset.StageTemplate => Resources.ExtractToTemp("st_dummy.afb"),
            InfrastructureAsset.NotesFieldTemplate => Resources.ExtractToTemp("nf_dummy.afb"),
            InfrastructureAsset.DummyAcb => Resources.ExtractToTemp("dummy.acb"),
            _ => throw new ArgumentOutOfRangeException(nameof(asset), asset, null)
        };
    }

    private string ResolveMuaExecutablePath()
    {
        if (Resources.HasResource("mua.exe"))
        {
            return Resources.ExtractToTemp("mua.exe");
        }

        if (Resources.HasResource("mua"))
        {
            return Resources.ExtractToTemp("mua");
        }

        return "mua";
    }
}
