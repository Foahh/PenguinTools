namespace PenguinTools.Core;

public readonly record struct OperationResult(bool Succeeded)
{
    public DiagnosticSnapshot Diagnostics { get; init; } = DiagnosticSnapshot.Empty;

    public static OperationResult Success()
    {
        return new OperationResult(true);
    }

    public static OperationResult Failure()
    {
        return new OperationResult(false);
    }

    public OperationResult WithDiagnostics(DiagnosticSnapshot diagnostics)
    {
        return this with { Diagnostics = diagnostics };
    }
}

public readonly record struct OperationResult<T>(bool Succeeded, T? Value)
{
    public DiagnosticSnapshot Diagnostics { get; init; } = DiagnosticSnapshot.Empty;

    public static OperationResult<T> Success(T value)
    {
        return new OperationResult<T>(true, value);
    }

    public static OperationResult<T> Failure()
    {
        return new OperationResult<T>(false, default);
    }

    public OperationResult ToResult()
    {
        return (Succeeded ? OperationResult.Success() : OperationResult.Failure()).WithDiagnostics(Diagnostics);
    }

    public OperationResult<T> WithDiagnostics(DiagnosticSnapshot diagnostics)
    {
        return this with { Diagnostics = diagnostics };
    }
}