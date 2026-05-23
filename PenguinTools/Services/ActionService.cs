using System.Media;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using PenguinTools.Core;
using PenguinTools.Core.Diagnostic;
using PenguinTools.i18n;

namespace PenguinTools.Services;

public partial class ActionService : ObservableObject, IProgress<ProgressReport>
{
    private readonly IDiagnosticsPresenter _diagnosticsPresenter;

    public ActionService(IDiagnosticsPresenter diagnosticsPresenter)
    {
        _diagnosticsPresenter = diagnosticsPresenter;
    }

    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial string Status { get; set; } = Strings.Status_Idle;
    [ObservableProperty] public partial DateTime StatusTime { get; set; } = DateTime.Now;
    [ObservableProperty] public partial string ProgressText { get; set; } = string.Empty;
    [ObservableProperty] public partial double ProgressValue { get; set; }
    [ObservableProperty] public partial bool IsProgressIndeterminate { get; set; }

    public bool CanRun()
    {
        return !IsBusy;
    }

    public Task RunAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        return RunAsync(async innerCt =>
        {
            await action(innerCt);
            return OperationResult.Success();
        }, ct);
    }

    public async Task RunAsync(Func<CancellationToken, Task<OperationResult>> action, CancellationToken ct = default)
    {
        await ExecuteAsync(action, ct);
    }

    private async Task<OperationResult> ExecuteAsync(Func<CancellationToken, Task<OperationResult>> action,
        CancellationToken ct)
    {
        if (!CanRun()) return OperationResult.Failure();
        var diagnostics = new DiagnosticCollector();
        var result = OperationResult.Failure();
        var wasCancelled = false;
        IsBusy = true;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            Report(new ProgressReport(Strings.Status_Starting));
            result = await Task.Run(() => action(cts.Token), cts.Token);

            SystemSounds.Exclamation.Play();
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
        }
        catch (Exception ex)
        {
            diagnostics.Report(ex);
        }
        finally
        {
            IsBusy = false;
        }

        result = result.WithDiagnostics(result.Diagnostics.Merge(DiagnosticSnapshot.Create(diagnostics)));
        if (wasCancelled) return result;

        if (result.Diagnostics.HasError)
        {
            Report(new ProgressReport(Strings.Status_Error));
        }
        else
        {
            Report(new ProgressReport(Strings.Status_Done, Completed: 1, Total: 1));
        }

        if (!result.Diagnostics.HasProblem) return result;

        _diagnosticsPresenter.Show(result.Diagnostics);
        return result;
    }

    public async Task<OperationResult<T>> RunAsync<T>(Func<CancellationToken, Task<OperationResult<T>>> action,
        CancellationToken ct = default)
    {
        var result = OperationResult<T>.Failure();
        var wrapper = await ExecuteAsync(async innerCt =>
        {
            result = await action(innerCt);
            return result.ToResult();
        }, ct);
        return result.WithDiagnostics(wrapper.Diagnostics);
    }

    public void Report(ProgressReport value)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => ApplyProgress(value));
            return;
        }

        ApplyProgress(value);
    }

    public void Report(string status, string? detail = null, int? completed = null, int? total = null)
    {
        Report(new ProgressReport(status, detail, completed, total));
    }

    private void ApplyProgress(ProgressReport value)
    {
        Status = value.Status;
        StatusTime = DateTime.Now;
        ProgressText = FormatProgressText(value);
        if (value.Percent is { } percent)
        {
            ProgressValue = percent;
            IsProgressIndeterminate = false;
        }
        else
        {
            ProgressValue = 0;
            IsProgressIndeterminate = IsBusy;
        }
    }

    private static string FormatProgressText(ProgressReport value)
    {
        var count = value is { Completed: { } completed, Total: { } total } ? $"{completed}/{total}" : null;

        if (!string.IsNullOrWhiteSpace(value.Detail) && count is not null)
            return $"{value.Detail} - {count}";

        if (!string.IsNullOrWhiteSpace(value.Detail)) return value.Detail;
        return count ?? string.Empty;
    }
}
