namespace PenguinTools.Core;

public interface IEmbeddedResourceStore
{
    string TempWorkPath { get; }

    bool HasResource(string resourceName);

    string GetTempPath(string fileName);

    string ExtractToTemp(string resourceName);

    Task CopyToAsync(string resourceName, string destinationPath, CancellationToken ct = default);

    Stream OpenRead(string resourceName);
}
