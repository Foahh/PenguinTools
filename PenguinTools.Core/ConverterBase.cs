namespace PenguinTools.Core;

public abstract class ConverterBase(IDiagnostic diag, IProgress<string>? prog = null)
{
    protected IDiagnostic Diagnostic { get; } = diag;
    protected IProgress<string>? Progress { get; } = prog;

    public async virtual Task ConvertAsync(CancellationToken ct = default)
    {
        await ValidateAsync(ct);
        if (Diagnostic.HasError) throw new OperationCanceledException();
        await ActionAsync(ct);
        if (Diagnostic.HasError) throw new OperationCanceledException();
    }

    protected abstract Task ActionAsync(CancellationToken ct = default);

    protected virtual Task ValidateAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

public abstract class ConverterBase<TResult>(IDiagnostic diag, IProgress<string>? prog = null) : ConverterBase(diag, prog)
{
    public async override Task<TResult> ConvertAsync(CancellationToken ct = default)
    {
        await ValidateAsync(ct);
        if (Diagnostic.HasError) throw new OperationCanceledException();
        var result = await ActionAsync(ct);
        if (Diagnostic.HasError) throw new OperationCanceledException();
        return result;
    }

    protected override abstract Task<TResult> ActionAsync(CancellationToken ct = default);
}