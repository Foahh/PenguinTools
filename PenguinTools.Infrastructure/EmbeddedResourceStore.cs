using System.Reflection;
using PenguinTools.Core;

namespace PenguinTools.Infrastructure;

public sealed class EmbeddedResourceStore : IResourceStore
{
    private readonly Assembly _assembly;
    private readonly string _sharedCachePath;

    public EmbeddedResourceStore(Assembly assembly, string tempWorkPath, string sharedCachePath)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempWorkPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sharedCachePath);

        _assembly = assembly;
        TempWorkPath = tempWorkPath;
        _sharedCachePath = sharedCachePath;
        Directory.CreateDirectory(TempWorkPath);
        Directory.CreateDirectory(_sharedCachePath);
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
        var cachedPath = Path.Combine(_sharedCachePath, fileName);
        var mutexName = $"Global\\PenguinTools_Asset_{SanitizeMutexName(fileName)}";

        using var mutex = new Mutex(false, mutexName);
        mutex.WaitOne();
        try
        {
            if (!File.Exists(cachedPath))
            {
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

    private static string SanitizeMutexName(string fileName)
    {
        return fileName.Replace('\\', '_').Replace('/', '_');
    }
}
