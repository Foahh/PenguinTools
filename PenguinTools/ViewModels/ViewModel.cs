using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Resources;
using PenguinTools.Models;
using PenguinTools.Services;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace PenguinTools.ViewModels;

public abstract partial class ViewModel : ObservableObject
{
    public ActionService ActionService { get; } = App.Services.GetRequiredService<ActionService>();
    public AssetManager AssetManager { get; } = App.Services.GetRequiredService<AssetManager>();

    protected static Dispatcher Dispatcher => Application.Current.Dispatcher;
    
    [RelayCommand]
    private static void OpenWiki()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Strings.Link_Documentation,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignored
        }
    }
}

public abstract class ActionViewModel : ViewModel
{
    protected ActionViewModel()
    {
        ActionCommand = new AsyncRelayCommand(Action, () => ActionService.CanRun() && CanRun());
        ActionService.PropertyChanged += (_, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            ActionCommand?.NotifyCanExecuteChanged();
        };
    }

    public IRelayCommand? ActionCommand { get; }

    protected abstract bool CanRun();
    protected abstract Task Action();
}

public abstract class ReloadableActionViewModel : ActionViewModel
{
    protected ReloadableActionViewModel()
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
    private FileSystemWatcher? fileWatcher;

    protected WatchViewModel()
    {
        ActionService.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);
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
        if (fileWatcher != null)
        {
            fileWatcher.Changed -= OnFileChanged;
            fileWatcher.Dispose();
            fileWatcher = null;
        }

        if (string.IsNullOrEmpty(value)) return;

        if (Directory.Exists(value))
        {
            fileWatcher = new FileSystemWatcher(value, FileGlob)
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
            fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.CreationTime
            };
        }

        if (fileWatcher == null) return;
        fileWatcher.Changed += OnFileChanged;
        fileWatcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!IsFileChanged(e.FullPath)) return;
        LastModifiedTime = DateTime.Now;
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

    protected async Task ReadModelInternal(IDiagnostic diag, IProgress<string> p, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ModelPath))
        {
            await Dispatcher.InvokeAsync(() => SetModel(Model, null));
            return;
        }
        p.Report(Strings.Status_reading);
        var model = await ReadModel(ModelPath, diag, p, ct);
        ct.ThrowIfCancellationRequested();
        await Dispatcher.InvokeAsync(() => SetModel(Model, model));
    }

    protected abstract Task<TModel> ReadModel(string path, IDiagnostic d, IProgress<string> p, CancellationToken ct = default);

    protected virtual void SetModel(TModel? oldMode, TModel? newModel)
    {
        Model = newModel;
    }

    protected override bool CanRun()
    {
        return Model != null;
    }

    protected async override Task Reload()
    {
        await ActionService.RunAsync(ReadModelInternal);
    }

    protected override bool CanReload()
    {
        return !string.IsNullOrWhiteSpace(ModelPath);
    }
}