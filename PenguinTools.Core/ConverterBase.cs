namespace PenguinTools.Core;

public abstract class ConverterBase(Diagnoster diag, IProgress<string>? prog = null)
{
    protected Diagnoster Diagnostic { get; } = diag;
    protected IProgress<string>? Progress { get; } = prog;

    public virtual async Task ConvertAsync(CancellationToken ct = default)
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

public abstract class ConverterBase<TResult>(Diagnoster diag, IProgress<string>? prog = null) : ConverterBase(diag, prog)
{
    public override async Task<TResult> ConvertAsync(CancellationToken ct = default)
    {
        await ValidateAsync(ct);
        if (Diagnostic.HasError) throw new OperationCanceledException();
        var result = await ActionAsync(ct);
        if (Diagnostic.HasError) throw new OperationCanceledException();
        return result;
    }

    protected abstract override Task<TResult> ActionAsync(CancellationToken ct = default);
}