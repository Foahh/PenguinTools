using CommunityToolkit.Mvvm.ComponentModel;
using PenguinTools.Controls;
using PenguinTools.Core;
using PenguinTools.Core.Resources;
using System.Media;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PenguinTools.Views;

namespace PenguinTools.Services;

public partial class ActionService : ObservableObject
{
    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial string Status { get; set; } = Strings.Status_Idle;
    [ObservableProperty] public partial DateTime StatusTime { get; set; } = DateTime.Now;

    public bool CanRun()
    {
        return !IsBusy;
    }

    public async Task RunAsync(Func<OperationContext, CancellationToken, Task> action, CancellationToken ct = default)
    {
        if (!CanRun()) return;
        var diagnostics = new Diagnoster();
        IsBusy = true;

        var progress = new Progress<string>(s =>
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Status = s;
                StatusTime = DateTime.Now;
            });
        });
        var context = new OperationContext(diagnostics, progress);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            context.ReportProgress(Strings.Status_Starting);
            await Task.Run(() => action(context, cts.Token), cts.Token);
            context.ReportProgress(Strings.Status_Done);

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

        if (!context.HasProblem) return;
        if (context.HasError) context.ReportProgress(Strings.Status_Error);

        var model = new DiagnosticsWindowViewModel
        {
            Diagnostics = [..diagnostics.Diagnostics]
        };
        var window = new DiagnosticsWindow
        {
            DataContext = model,
            Owner = App.ServiceProvider.GetRequiredService<MainWindow>()
        };
        window.ShowDialog();
    }
}
