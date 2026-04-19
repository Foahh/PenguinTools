using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PenguinTools.Core.Asset;
using PenguinTools.Resources;

namespace PenguinTools.ViewModels;

public partial class UserAssetSetupViewModel : ObservableObject
{
    private readonly AssetManager _assetManager;
    private readonly Window _window;

    public UserAssetSetupViewModel(AssetManager assetManager, Window window, string explanationText)
    {
        _assetManager = assetManager;
        _window = window;
        ExplanationText = explanationText;
    }

    public string ExplanationText { get; }

    [RelayCommand]
    private async Task BrowseGameFolderAsync()
    {
        var dlg = new OpenFolderDialog
        {
            Title = Strings.Title_Select_the_game_folder,
            Multiselect = false,
            ValidateNames = true
        };

        if (dlg.ShowDialog(_window) != true || string.IsNullOrWhiteSpace(dlg.FolderName))
        {
            return;
        }

        try
        {
            await _assetManager.CollectAssetsAsync(dlg.FolderName, CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                _window,
                ex.Message,
                Strings.Title_Error,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        _window.Close();
    }

    [RelayCommand]
    private void Skip()
    {
        _window.Close();
    }
}
