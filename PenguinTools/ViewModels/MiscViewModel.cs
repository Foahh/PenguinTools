using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.i18n;
using PenguinTools.Infrastructure;
using PenguinTools.Media;
using PenguinTools.Services;

namespace PenguinTools.ViewModels;

public partial class MiscViewModel : ViewModel
{
    private readonly IApplicationPaths _paths;
    private readonly ResourceStoreOptions _storeOptions;
    private readonly IGameAssetService _gameAssetService;

    public MiscViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        IExternalLauncher externalLauncher,
        IApplicationPaths paths,
        ResourceStoreOptions storeOptions,
        IGameAssetService gameAssetService)
        : base(actionService, assetManager, mediaTool, resourceStore, assetProvider, externalLauncher)
    {
        _paths = paths;
        _storeOptions = storeOptions;
        _gameAssetService = gameAssetService;
    }

    [RelayCommand]
    private void OpenTempDirectory()
    {
        ShellExplorer.OpenDirectory(ResourceStore.TempWorkPath);
    }

    [RelayCommand]
    private void OpenUserDataDirectory()
    {
        ShellExplorer.OpenDirectory(_paths.UserDataPath);
    }

    [RelayCommand]
    private void OpenAssetsDirectory()
    {
        ShellExplorer.OpenDirectory(
            ResourceStoreFactory.ResolveInfrastructureAssetsPath(_storeOptions, _paths));
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
        if (result is not true || string.IsNullOrWhiteSpace(openDlg.FileName)) return;

        var baseDir = Path.GetDirectoryName(openDlg.FileName);
        var saveDlg = new OpenFolderDialog
        {
            FolderName = Path.GetFileNameWithoutExtension(openDlg.FileName),
            InitialDirectory = baseDir != null ? new DirectoryInfo(baseDir).Name : null,
            Title = Strings.Title_Select_the_output_folder,
            Multiselect = false,
            ValidateNames = true
        };
        if (saveDlg.ShowDialog() != true) return;

        await ActionService.RunAsync(async ct =>
        {
            var extractor = new AfbExtractor(new AfbExtractRequest(openDlg.FileName, saveDlg.FolderName), MediaTool);
            return await extractor.ExtractAsync(ct);
        });
    }

    [RelayCommand]
    private async Task CollectAsset()
    {
        var gameDirectory = await _gameAssetService.BrowseGameDirectoryAsync(Application.Current.MainWindow);
        if (string.IsNullOrWhiteSpace(gameDirectory)) return;

        await ActionService.RunAsync(ct => _gameAssetService.CollectAssetsAsync(gameDirectory, ct));
    }
}
