using PenguinTools.Core;

namespace PenguinTools.Infrastructure;

public sealed class FileResourceStore : IResourceStore
{
    private readonly string _assetRootPath;
    private readonly Lock _lock = new();

    public FileResourceStore(string assetRootPath, string tempWorkPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempWorkPath);

        _assetRootPath = assetRootPath;
        TempWorkPath = tempWorkPath;
        Directory.CreateDirectory(TempWorkPath);
    }

    public string TempWorkPath { get; }

    public bool HasResource(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        return File.Exists(Path.Combine(_assetRootPath, resourceName));
    }

    public string GetTempPath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        Directory.CreateDirectory(TempWorkPath);
        return Path.Combine(TempWorkPath, fileName);
    }

    public string ExtractToTemp(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        var sourcePath = GetResourcePath(resourceName);
        var destinationPath = GetTempPath(Path.GetFileName(resourceName));

        lock (_lock)
        {
            if (!Path.GetFullPath(sourcePath).Equals(Path.GetFullPath(destinationPath), StringComparison.Ordinal))
                File.Copy(sourcePath, destinationPath, true);

            ResourceStoreHelpers.EnsureExecutableIfNeeded(destinationPath, resourceName);
        }

        return destinationPath;
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
        return File.OpenRead(GetResourcePath(resourceName));
    }

    public void Dispose()
    {
        ResourceStoreHelpers.ClearDirectory(TempWorkPath, true);
    }

    private string GetResourcePath(string resourceName)
    {
        var path = Path.Combine(_assetRootPath, resourceName);
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Resource '{resourceName}' was not found in asset directory '{_assetRootPath}'.",
                path);

        return path;
    }
}