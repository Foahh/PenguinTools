namespace PenguinTools.Core;

public readonly record struct OperationResult(bool Succeeded)
{
    public static OperationResult Success() => new(true);

    public static OperationResult Failure() => new(false);
}

public readonly record struct OperationResult<T>(bool Succeeded, T? Value)
{
    public static OperationResult<T> Success(T value) => new(true, value);

    public static OperationResult<T> Failure() => new(false, default);

    public OperationResult ToResult() => Succeeded ? OperationResult.Success() : OperationResult.Failure();
}
