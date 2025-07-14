using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Media;

public class AfbExtractor : IConverter<AfbExtractor.Options>
{
    public async Task ConvertAsync(Options ctx, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!await CanConvertAsync(ctx, diag)) return;
        progress?.Report(Strings.Status_extracting);
        await Manipulate.ExtractDdsAsync(ctx.InputPath, ctx.DestinationFolder, ct);
        ct.ThrowIfCancellationRequested();
        progress?.Report(Strings.Status_writing);
    }

    public Task<bool> CanConvertAsync(Options options, IDiagnostic diag)
    {
        if (!File.Exists(options.InputPath)) diag.Report(Severity.Error, Strings.Error_file_not_found, options.InputPath);
        return Task.FromResult(!diag.HasError);
    }

    public record Options(string InputPath, string DestinationFolder);
}