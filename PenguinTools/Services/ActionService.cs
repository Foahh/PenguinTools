using CommunityToolkit.Mvvm.ComponentModel;
using PenguinTools.Controls;
using PenguinTools.Core;
using PenguinTools.Core.Resources;
using System.Media;
using System.Windows;
using System.Windows.Threading;

namespace PenguinTools.Services;

public partial class ActionService : ObservableObject
{
    protected static Dispatcher Dispatcher => Application.Current.Dispatcher;

    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial string Status { get; set; } = Strings.Status_Idle;
    [ObservableProperty] public partial DateTime StatusTime { get; set; } = DateTime.Now;

    public bool CanRun()
    {
        return !IsBusy;
    }

    public async Task RunAsync(Func<Diagnoster, IProgress<string>?, CancellationToken, Task> action, CancellationToken ct = default)
    {
        if (!CanRun()) return;
        var diagnostics = new Diagnoster();
        IsBusy = true;

        var progress = new Progress<string>(s =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                Status = s;
                StatusTime = DateTime.Now;
            });
        });
        IProgress<string> ip = progress;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            ip.Report(Strings.Status_Starting);
            await Task.Run(() => action(diagnostics, progress, cts.Token), cts.Token);
            ip.Report(Strings.Status_Done);

            SystemSounds.Exclamation.Play();
        }
        catch (OperationCanceledException)
        {
            // do nothing
        }
        catch (Exception ex)
        {
            diagnostics.Report(ex);
        }
        finally
        {
            IsBusy = false;
        }

        if (!diagnostics.HasProblem) return;
        if (diagnostics.HasError) ip.Report(Strings.Status_Error);

        var model = new DiagnosticsWindowViewModel
        {
            Diagnostics = [..diagnostics.Diagnostics]
        };
        var window = new DiagnosticsWindow
        {
            DataContext = model,
            Owner = App.MainWindow
        };
        window.ShowDialog();
    }
}