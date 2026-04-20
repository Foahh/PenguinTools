using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PenguinTools.Resources;
using PenguinTools.Services;

namespace PenguinTools.ViewModels;

public partial class UserAssetSetupViewModel : ObservableObject
{
    private readonly IGameAssetService _gameAssetService;
    private readonly Window _window;

    public UserAssetSetupViewModel(IGameAssetService gameAssetService, Window window, string explanationText)
    {
        _gameAssetService = gameAssetService;
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
        var gameDirectory = await _gameAssetService.BrowseGameDirectoryAsync(_window);
        if (string.IsNullOrWhiteSpace(gameDirectory)) return;

        try
        {
            IsProcessing = true;
            await _gameAssetService.CollectAssetsAsync(gameDirectory, CancellationToken.None).ConfigureAwait(true);
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
