using CommunityToolkit.Mvvm.ComponentModel;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Media;
using PenguinTools.Resources;
using PenguinTools.Infrastructure;
using PenguinTools.Core.Metadata;
using PenguinTools.Models;
using PenguinTools.Services;
using System.IO;

namespace PenguinTools.ViewModels;

public partial class OptionViewModel : WatchViewModel<OptionModel>
{
    private readonly IFileDialogService _fileDialogs;
    private readonly IChartScanService _chartScan;
    private readonly IOptionService _export;

    public OptionViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IEmbeddedResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        IExternalLauncher externalLauncher,
        IFileDialogService fileDialogs,
        IChartScanService chartScan,
        IOptionService export)
        : base(actionService, assetManager, mediaTool, resourceStore, assetProvider, externalLauncher)
    {
        _fileDialogs = fileDialogs;
        _chartScan = chartScan;
        _export = export;
    }

    [ObservableProperty]
    public partial Book? SelectedBook { get; set; }

    [ObservableProperty]
    public partial BookItem? SelectedBookItem { get; set; }

    protected override string FileGlob => "*.mgxc";

    protected override bool IsFileChanged(string path) =>
        Model?.Books.Values
            .SelectMany(book => book.Items.Values)
            .Any(item => string.Equals(item.Meta.FilePath, path, StringComparison.OrdinalIgnoreCase)) == true;

    protected override void SetModel(OptionModel? oldModel, OptionModel? newModel)
    {
        base.SetModel(oldModel, newModel);
        SelectedBook = null;
        SelectedBookItem = null;
    }

    public void ApplyPreviewTreeSelection(object? newValue)
    {
        if (newValue is KeyValuePair<int, Book> kvpBook)
        {
            SelectedBookItem = null;
            SelectedBook = kvpBook.Value;
        }
        else if (newValue is KeyValuePair<Difficulty, BookItem> kvpBookItem)
        {
            SelectedBookItem = kvpBookItem.Value;
            SelectedBook = null;
        }
        else
        {
            SelectedBookItem = null;
            SelectedBook = null;
        }
    }

    protected override async Task<OperationResult<OptionModel>> ReadModel(string path, CancellationToken ct = default)
    {
        var diagnostics = OptionParallelBatch.CreateDiagnoster();
        await Dispatcher.InvokeAsync(() =>
        {
            ActionService.Status = Strings.Status_Searching;
            ActionService.StatusTime = DateTime.Now;
        });
        var model = await LoadModelAsync(path, ct);
        var scanParams = new ChartScanParameters(FileGlob, diagnostics, model.BatchSize, model.WorkingDirectory);
        var scanResult = await _chartScan.ScanAsync(path, model.Books, scanParams, ct);

        if (model.Books.Count == 0) throw new DiagnosticException(Strings.Error_No_charts_are_found_directory);
        await Dispatcher.InvokeAsync(() =>
        {
            ActionService.Status = Strings.Status_Done;
            ActionService.StatusTime = DateTime.Now;
        });

        return OperationResult<OptionModel>.Success(model).WithDiagnostics(scanResult.Diagnostics);
    }

    protected override async Task<OperationResult> Action(CancellationToken ct = default)
    {
        var settings = Model;
        if (settings == null) return OperationResult.Success();
        if (!settings.CanExecute) throw new DiagnosticException(Strings.Error_Noop);

        if (settings.Books.Count == 0) throw new DiagnosticException(Strings.Error_No_charts_are_found_directory);

        var workingDirectory = await SelectOutputDirectoryAsync(settings);
        if (workingDirectory == null) return OperationResult.Success();

        settings.WorkingDirectory = workingDirectory;
        var outputPaths = ExportOutputPaths.FromOptionDirectory(settings.OptionDirectory);

        await settings.SaveAsync(ModelPath, ct);

        return await _export.ExportAsync(settings, outputPaths, ct);
    }

    private async Task<OptionModel> LoadModelAsync(string path, CancellationToken ct)
    {
        var model = new OptionModel();
        await model.LoadAsync(path, ct);

        if (string.IsNullOrWhiteSpace(model.WorkingDirectory) || !Directory.Exists(model.WorkingDirectory))
        {
            model.WorkingDirectory = path;
        }

        return model;
    }

    private Task<string?> SelectOutputDirectoryAsync(OptionModel settings) =>
        _fileDialogs.PickFolderAsync(
            Strings.Title_Select_the_output_folder,
            GetInitialOutputDirectory(settings),
            new Guid("C81454B6-EA09-41D6-90B2-4BD4FB3D5449"));

    private string GetInitialOutputDirectory(OptionModel settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.WorkingDirectory) && Directory.Exists(settings.WorkingDirectory))
        {
            return settings.WorkingDirectory;
        }

        var modelDirectory = Path.GetDirectoryName(ModelPath);
        return !string.IsNullOrWhiteSpace(modelDirectory) && Directory.Exists(modelDirectory)
            ? modelDirectory
            : string.Empty;
    }

    protected override async Task Reload()
    {
        if (Model is IPersistable persistable && !string.IsNullOrWhiteSpace(ModelPath))
            await persistable.SaveAsync(ModelPath);

        await base.Reload();
    }
}
