using CommunityToolkit.Mvvm.ComponentModel;
using PenguinTools.Core;
using PenguinTools.Resources;
using System.Media;

namespace PenguinTools.Services;

public partial class ActionService : ObservableObject
{
    private readonly IDiagnosticsPresenter _diagnosticsPresenter;

    public ActionService(IDiagnosticsPresenter diagnosticsPresenter)
    {
        _diagnosticsPresenter = diagnosticsPresenter;
    }

    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial string Status { get; set; } = Strings.Status_Idle;
    [ObservableProperty] public partial DateTime StatusTime { get; set; } = DateTime.Now;

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

    private async Task<OperationResult> ExecuteAsync(Func<CancellationToken, Task<OperationResult>> action, CancellationToken ct)
    {
        if (!CanRun()) return OperationResult.Failure();
        var diagnostics = new Diagnoster();
        OperationResult result = OperationResult.Failure();
        var wasCancelled = false;
        IsBusy = true;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            Status = Strings.Status_Starting;
            StatusTime = DateTime.Now;
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
            Status = Strings.Status_Error;
            StatusTime = DateTime.Now;
        }
        else
        {
            Status = Strings.Status_Done;
            StatusTime = DateTime.Now;
        }

        if (!result.Diagnostics.HasProblem) return result;

        _diagnosticsPresenter.Show(result.Diagnostics);
        return result;
    }

    public async Task<OperationResult<T>> RunAsync<T>(Func<CancellationToken, Task<OperationResult<T>>> action, CancellationToken ct = default)
    {
        OperationResult<T> result = OperationResult<T>.Failure();
        var wrapper = await ExecuteAsync(async innerCt =>
        {
            result = await action(innerCt);
            return result.ToResult();
        }, ct);
        return result.WithDiagnostics(wrapper.Diagnostics);
    }
}
