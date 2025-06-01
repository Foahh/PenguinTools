namespace PenguinTools.Common;

public interface IConverter<in T>
{
    public Task ConvertAsync(T options, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default);
    public Task<bool> CanConvertAsync(T options, IDiagnostic diag);
}