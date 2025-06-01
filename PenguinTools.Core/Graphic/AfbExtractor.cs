using PenguinTools.Common.Resources;

namespace PenguinTools.Common.Graphic;

public class AfbExtractor : IConverter<AfbExtractor.Options>
{
    public async Task ConvertAsync(Options options, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!await CanConvertAsync(options, diag)) return;
        progress?.Report(Strings.Status_extracting);
        MuaInterop.ExtractAfb(options.InputPath, options.OutputFolder);
        ct.ThrowIfCancellationRequested();
        progress?.Report(Strings.Status_writing);
    }

    public Task<bool> CanConvertAsync(Options options, IDiagnostic diag)
    {
        if (!File.Exists(options.InputPath)) diag.Report(Severity.Error, Strings.Error_file_not_found, options.InputPath);
        return Task.FromResult(!diag.HasError);
    }

    public record Options(string InputPath, string OutputFolder);
}