using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Media;

public class AfbExtractor(Diagnoster diag, IProgress<string>? prog = null) : ConverterBase(diag, prog)
{
    public required string InPath { get; init; }
    public required string OutFolder { get; init; }

    protected override async Task ActionAsync(CancellationToken ct = default)
    {
        Progress?.Report(Strings.Status_Extracting);
        await Manipulate.ExtractDdsAsync(InPath, OutFolder, ct);
        ct.ThrowIfCancellationRequested();
        Progress?.Report(Strings.Status_Writing);
    }

    protected override Task ValidateAsync(CancellationToken ct = default)
    {
        if (!File.Exists(InPath)) Diagnostic.Report(Severity.Error, Strings.Error_File_not_found, InPath);
        return Task.CompletedTask;
    }
}