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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BrowseGameFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(SkipCommand))]
    public partial bool IsProcessing { get; set; }

    public string ProcessingText => Strings.UserAssetSetup_Processing;

    [RelayCommand(CanExecute = nameof(CanBrowseGameFolder))]
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
            IsProcessing = true;
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
        finally
        {
            IsProcessing = false;
        }

        _window.Close();
    }

    private bool CanBrowseGameFolder()
    {
        return !IsProcessing;
    }

    [RelayCommand(CanExecute = nameof(CanSkip))]
    private void Skip()
    {
        _window.Close();
    }

    private bool CanSkip()
    {
        return !IsProcessing;
    }
}
