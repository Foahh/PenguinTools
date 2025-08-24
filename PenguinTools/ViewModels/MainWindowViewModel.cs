using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PenguinTools.Core.Resources;
using PenguinTools.Services;
using System.Diagnostics;

namespace PenguinTools.ViewModels;

public partial class MainWindowViewModel : ViewModel
{
    private readonly IUpdateService _updateService;

    public MainWindowViewModel(IUpdateService updateService)
    {
        this._updateService = updateService;
        ActionService.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);
    }

    public bool IsUpdateAvailable => LatestVersion != null && LatestVersion > App.Version;

    [ObservableProperty]
    public partial string? DownloadUrl { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadUpdateCommand))]
    public partial Version? LatestVersion { get; set; }

    [ObservableProperty]
    public partial string UpdateStatus { get; set; } = string.Empty;

    public string Status => ActionService.Status;
    public DateTime StatusTime => ActionService.StatusTime;

    [RelayCommand]
    public async Task UpdateCheck()
    {
        UpdateStatus = Strings.Update_Checking;
        try
        {
            var (result, url) = await _updateService.CheckForUpdatesAsync();
            LatestVersion = result;
            DownloadUrl = url;
            UpdateStatus = IsUpdateAvailable ? string.Format(Strings.Update_New_Version_Available, LatestVersion.ToString(3)) : Strings.Update_Already_Latest;
        }
        catch
        {
            UpdateStatus = Strings.Update_Failed;
            LatestVersion = null;
            DownloadUrl = null;
        }
    }

    [RelayCommand(CanExecute = nameof(IsUpdateAvailable))]
    private void DownloadUpdate()
    {
        if (string.IsNullOrWhiteSpace(DownloadUrl)) return;
        Process.Start(new ProcessStartInfo
        {
            FileName = DownloadUrl,
            UseShellExecute = true
        });
    }
}