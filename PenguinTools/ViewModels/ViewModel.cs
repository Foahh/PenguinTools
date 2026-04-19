using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Media;
using PenguinTools.Resources;
using PenguinTools.Infrastructure;
using PenguinTools.Models;
using PenguinTools.Services;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace PenguinTools.ViewModels;

public abstract partial class ViewModel : ObservableObject
{
    private readonly IExternalLauncher _externalLauncher;

    public ActionService ActionService { get; }
    public AssetManager AssetManager { get; }
    public IMediaTool MediaTool { get; }
    public IEmbeddedResourceStore ResourceStore { get; }
    public IInfrastructureAssetProvider AssetProvider { get; }

    protected ViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IEmbeddedResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        IExternalLauncher externalLauncher)
    {
        ActionService = actionService;
        AssetManager = assetManager;
        MediaTool = mediaTool;
        ResourceStore = resourceStore;
        AssetProvider = assetProvider;
        _externalLauncher = externalLauncher;
    }

    protected static Dispatcher Dispatcher => Application.Current.Dispatcher;

    [RelayCommand]
    private void OpenWiki() => _externalLauncher.Launch(Strings.Link_Documentation);
}

public abstract class ActionViewModel : ViewModel
{
    protected ActionViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IEmbeddedResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        IExternalLauncher externalLauncher)
        : base(actionService, assetManager, mediaTool, resourceStore, assetProvider, externalLauncher)
    {
        ActionCommand = new AsyncRelayCommand(() => ActionService.RunAsync(Action), () => ActionService.CanRun() && CanRun());
        ActionService.PropertyChanged += (_, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            ActionCommand?.NotifyCanExecuteChanged();
        };
    }

    public IRelayCommand? ActionCommand { get; }

    protected abstract bool CanRun();
    protected abstract Task<OperationResult> Action(CancellationToken ct = default);
}

public abstract class ReloadableActionViewModel : ActionViewModel
{
    protected ReloadableActionViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IEmbeddedResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        IExternalLauncher externalLauncher)
        : base(actionService, assetManager, mediaTool, resourceStore, assetProvider, externalLauncher)
    {
        ReloadCommand = new AsyncRelayCommand(Reload, () => ActionService.CanRun() && CanReload());
        ActionService.PropertyChanged += (_, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            ReloadCommand?.NotifyCanExecuteChanged();
        };
    }

    public IRelayCommand? ReloadCommand { get; }

    protected abstract bool CanReload();
    protected abstract Task Reload();
}

public abstract partial class WatchViewModel<TModel> : ReloadableActionViewModel where TModel : Model
{
    private FileSystemWatcher? _fileWatcher;
    private bool _pendingReload;

    protected WatchViewModel(
        ActionService actionService,
        AssetManager assetManager,
        IMediaTool mediaTool,
        IEmbeddedResourceStore resourceStore,
        IInfrastructureAssetProvider assetProvider,
        IExternalLauncher externalLauncher)
        : base(actionService, assetManager, mediaTool, resourceStore, assetProvider, externalLauncher)
    {
        ActionService.PropertyChanged += OnWatchViewModelActionServicePropertyChanged;
    }

    private void OnWatchViewModelActionServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
        if (e.PropertyName != nameof(ActionService.IsBusy) || ActionService.IsBusy || !_pendingReload) { return; }

        ConsiderEnqueueReloadFromFileWatch();
    }

    private void ConsiderEnqueueReloadFromFileWatch()
    {
        if (!CanReload()) return;
        if (!ActionService.CanRun())
        {
            _pendingReload = true;
            return;
        }

        _pendingReload = false;
        _ = ActionService.RunAsync(ReadModelInternal);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ActionCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReloadCommand))]
    public partial string ModelPath { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ActionCommand))]
    public partial TModel? Model { get; set; }

    [ObservableProperty]
    public partial DateTime? LastModifiedTime { get; set; }

    protected virtual string FileGlob => "*.*";

    partial void OnModelPathChanged(string value)
    {
        _ = ActionService.RunAsync(ReadModelInternal);
    }

    private void InitializeWatcher(string value)
    {
        if (_fileWatcher != null)
        {
            _fileWatcher.Changed -= OnFileChanged;
            _fileWatcher.Dispose();
            _fileWatcher = null;
        }

        if (string.IsNullOrEmpty(value)) return;

        if (Directory.Exists(value))
        {
            _fileWatcher = new FileSystemWatcher(value, FileGlob)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.CreationTime
            };
        }

        else if (File.Exists(value))
        {
            var directory = Path.GetDirectoryName(value);
            var fileName = Path.GetFileName(value);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName)) return;
            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.CreationTime
            };
        }

        if (_fileWatcher == null) return;
        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!IsFileChanged(e.FullPath)) return;
        LastModifiedTime = DateTime.Now;
        _ = Dispatcher.InvokeAsync(ConsiderEnqueueReloadFromFileWatch);
    }

    protected virtual bool IsFileChanged(string path)
    {
        return path == ModelPath;
    }

    partial void OnModelChanged(TModel? value)
    {
        InitializeWatcher(ModelPath);
        LastModifiedTime = null;
    }

    protected async Task<OperationResult> ReadModelInternal(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ModelPath))
        {
            await Dispatcher.InvokeAsync(() => SetModel(Model, null));
            return OperationResult.Success();
        }
        await Dispatcher.InvokeAsync(() =>
        {
            ActionService.Status = Strings.Status_Reading;
            ActionService.StatusTime = DateTime.Now;
        });
        var model = await ReadModel(ModelPath, ct);
        if (!model.Succeeded || model.Value is not { } value) return model.ToResult();
        ct.ThrowIfCancellationRequested();
        await Dispatcher.InvokeAsync(() => SetModel(Model, value));
        return model.ToResult();
    }

    protected abstract Task<OperationResult<TModel>> ReadModel(string path, CancellationToken ct = default);

    protected virtual void SetModel(TModel? oldMode, TModel? newModel)
    {
        Model = newModel;
    }

    protected override bool CanRun()
    {
        return Model != null;
    }

    protected override async Task Reload()
    {
        await ActionService.RunAsync(ReadModelInternal);
    }

    protected override bool CanReload()
    {
        return !string.IsNullOrWhiteSpace(ModelPath);
    }
}
