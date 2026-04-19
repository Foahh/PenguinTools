using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Infrastructure;
using PenguinTools.Media;
using PenguinTools.Resources;
using PenguinTools.Services;

namespace PenguinTools.ViewModels;

public partial class MiscViewModel : ViewModel
{
    public MiscViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        IExternalLauncher externalLauncher)
        : base(actionService, assetManager, mediaTool, resourceStore, assetProvider, externalLauncher)
    {
    }

    [RelayCommand]
    private void OpenTempDirectory()
    {
        var path = ResourceStore.TempWorkPath;

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = path,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private async Task ExtractAfbFile()
    {
        var openDlg = new OpenFileDialog
        {
            Title = Strings.Title_Select_the_input_file,
            Filter = Strings.Filefilter_afb,
            CheckFileExists = true,
            AddExtension = true,
            ValidateNames = true
        };
        var result = openDlg.ShowDialog(Application.Current.MainWindow);
        if (result is not true || string.IsNullOrWhiteSpace(openDlg.FileName)) { return; }

        var baseDir = Path.GetDirectoryName(openDlg.FileName);
        var saveDlg = new OpenFolderDialog
        {
            FolderName = Path.GetFileNameWithoutExtension(openDlg.FileName),
            InitialDirectory = baseDir != null ? new DirectoryInfo(baseDir).Name : null,
            Title = Strings.Title_Select_the_output_folder,
            Multiselect = false,
            ValidateNames = true
        };
        if (saveDlg.ShowDialog() != true) { return; }

        await ActionService.RunAsync(async ct =>
        {
            var extractor = new AfbExtractor(new AfbExtractRequest(openDlg.FileName, saveDlg.FolderName), MediaTool);
            return await extractor.ExtractAsync(ct);
        });
    }

    [RelayCommand]
    private async Task CollectAsset()
    {
        var openDlg = new OpenFolderDialog
        {
            Title = Strings.Title_Select_the_game_folder,
            Multiselect = false,
            ValidateNames = true
        };
        var result = openDlg.ShowDialog(Application.Current.MainWindow);
        if (result is not true || string.IsNullOrWhiteSpace(openDlg.FolderName)) { return; }

        await ActionService.RunAsync(ct => AssetManager.CollectAssetsAsync(openDlg.FolderName, ct));
    }
}
