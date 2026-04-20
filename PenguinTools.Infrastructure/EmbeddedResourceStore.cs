using System.Reflection;
using PenguinTools.Core;

namespace PenguinTools.Infrastructure;

public sealed class EmbeddedResourceStore : IResourceStore
{
    private readonly Assembly _assembly;
    private readonly string? _sharedCachePath;
    private readonly Lock _lock = new();

    public EmbeddedResourceStore(Assembly assembly, string tempWorkPath, string? sharedCachePath = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempWorkPath);

        _assembly = assembly;
        _sharedCachePath = sharedCachePath;
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

        var fileName = Path.GetFileName(resourceName);

        if (_sharedCachePath != null)
        {
            var cachedPath = Path.Combine(_sharedCachePath, fileName);
            var mutexName = $"Global\\PenguinTools_Asset_{fileName}";

            using var mutex = new Mutex(false, mutexName);
            mutex.WaitOne();
            try
            {
                if (!File.Exists(cachedPath))
                {
                    Directory.CreateDirectory(_sharedCachePath);
                    using var stream = OpenRead(resourceName);
                    using var fileStream = File.Create(cachedPath);
                    stream.CopyTo(fileStream);
                    ResourceStoreHelpers.EnsureExecutableIfNeeded(cachedPath, resourceName);
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            return cachedPath;
        }

        var path = GetTempPath(fileName);
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
        ResourceStoreHelpers.ClearDirectory(TempWorkPath, true);
    }
}