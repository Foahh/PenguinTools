using System.Windows;
using Microsoft.Win32;
using PenguinTools.Views;

namespace PenguinTools.Services;

public sealed class FileDialogService : IFileDialogService
{
    private readonly Lazy<MainWindow> _mainWindow;

    public FileDialogService(Lazy<MainWindow> mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public Task<string?> PickFolderAsync(string title, string? initialDirectory, Guid? clientGuid = null)
    {
        var path = Application.Current.Dispatcher.Invoke(() =>
        {
            var dlg = new OpenFolderDialog
            {
                Title = title,
                Multiselect = false,
                ValidateNames = true
            };
            if (!string.IsNullOrWhiteSpace(initialDirectory)) dlg.InitialDirectory = initialDirectory;
            if (clientGuid is { } g) dlg.ClientGuid = g;
            return dlg.ShowDialog(_mainWindow.Value) == true ? dlg.FolderName : null;
        });
        return Task.FromResult(path);
    }
}