using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using PenguinTools.Core.Asset;
using PenguinTools.Resources;

namespace PenguinTools.Services;

public sealed class GameAssetService : IGameAssetService
{
    private readonly AssetManager _assetManager;
    private readonly IUiSettingsService _uiSettingsService;

    public GameAssetService(AssetManager assetManager, IUiSettingsService uiSettingsService)
    {
        _assetManager = assetManager;
        _uiSettingsService = uiSettingsService;
    }

    public string? GameDirectory =>
        string.IsNullOrWhiteSpace(_uiSettingsService.Settings.GameDirectory) ? null : _uiSettingsService.Settings.GameDirectory;

    public Task<string?> BrowseGameDirectoryAsync(Window? owner = null)
    {
        var path = Application.Current.Dispatcher.Invoke(() =>
        {
            var dlg = new OpenFolderDialog
            {
                Title = Strings.Title_Select_the_game_folder,
                Multiselect = false,
                ValidateNames = true
            };
            if (!string.IsNullOrWhiteSpace(GameDirectory)) dlg.InitialDirectory = GameDirectory;
            return dlg.ShowDialog(owner) == true ? dlg.FolderName : null;
        });
        return Task.FromResult(path);
    }

    public async Task CollectAssetsAsync(string directory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directory)) return;

        var normalizedDirectory = Path.TrimEndingDirectorySeparator(directory.Trim());
        if (!Directory.Exists(normalizedDirectory)) return;

        _uiSettingsService.Settings.GameDirectory = normalizedDirectory;
        await _uiSettingsService.SaveAsync(cancellationToken);
        await _assetManager.CollectAssetsAsync(normalizedDirectory, cancellationToken);
    }

    public async Task AutoCollectAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(GameDirectory) || !Directory.Exists(GameDirectory)) return;

        try
        {
            await _assetManager.CollectAssetsAsync(GameDirectory, cancellationToken);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}
