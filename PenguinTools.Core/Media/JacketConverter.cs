using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Media;

public class JacketConverter
{
    public JacketConverter(JacketConvertRequest request, Diagnoster diag, IProgress<string>? prog = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(diag);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutPath);

        Diagnostic = diag;
        Progress = prog;
        InPath = request.InPath;
        OutPath = request.OutPath;
    }

    private Diagnoster Diagnostic { get; }
    private IProgress<string>? Progress { get; }
    private string InPath { get; }
    private string OutPath { get; }

    public async Task<bool> ConvertAsync(CancellationToken ct = default)
    {
        if (!Validate()) return false;

        Progress?.Report(Strings.Status_Converting_jacket);
        ct.ThrowIfCancellationRequested();
        await Manipulate.ConvertJacketAsync(InPath, OutPath, ct);
        ct.ThrowIfCancellationRequested();
        return !Diagnostic.HasError;
    }

    private bool Validate()
    {
        if (!File.Exists(InPath)) Diagnostic.Report(Severity.Error, Strings.Error_Jacket_file_not_found, InPath);
        return !Diagnostic.HasError;
    }
}
