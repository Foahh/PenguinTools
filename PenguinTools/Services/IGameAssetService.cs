using System.Windows;

namespace PenguinTools.Services;

public interface IGameAssetService
{
    string? GameDirectory { get; }

    Task<string?> BrowseGameDirectoryAsync(Window? owner = null);

    Task CollectAssetsAsync(string directory, CancellationToken cancellationToken = default);

    Task AutoCollectAsync(CancellationToken cancellationToken = default);
}
