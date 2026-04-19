using System.Reflection;
using PenguinTools.Core;

namespace PenguinTools.Infrastructure;

public sealed class EmbeddedResourceStore : IResourceStore
{
    private readonly Assembly _assembly;
    private readonly Lock _lock = new();

    public EmbeddedResourceStore(Assembly assembly, string tempWorkPath)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempWorkPath);

        _assembly = assembly;
        TempWorkPath = tempWorkPath;
        Directory.CreateDirectory(TempWorkPath);
    }

    public string TempWorkPath { get; }

    public bool HasResource(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        return _assembly.GetManifestResourceInfo(resourceName) is not null;
    }

    public string GetTempPath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return Path.Combine(TempWorkPath, fileName);
    }

    public string ExtractToTemp(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        var path = GetTempPath(Path.GetFileName(resourceName));

        lock (_lock)
        {
            using var stream = OpenRead(resourceName);
            using var fileStream = File.Create(path);
            stream.CopyTo(fileStream);
        }

        ResourceStoreHelpers.EnsureExecutableIfNeeded(path, resourceName);

        return path;
    }

    public async Task CopyToAsync(string resourceName, string destinationPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        await using var stream = OpenRead(resourceName);
        await using var fileStream = File.Create(destinationPath);
        await stream.CopyToAsync(fileStream, ct);
        ResourceStoreHelpers.EnsureExecutableIfNeeded(destinationPath, resourceName);
    }

    public Stream OpenRead(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        return _assembly.GetManifestResourceStream(resourceName)
               ?? throw new FileNotFoundException(
                   $"Resource '{resourceName}' was not found in assembly '{_assembly.GetName().Name}'.");
    }

    public void Dispose()
    {
        ResourceStoreHelpers.ClearDirectory(TempWorkPath, deleteRoot: true);
    }
}
