using PenguinTools.Core.Media.Resources;

namespace PenguinTools.Core.Media;

public class AfbExtractor
{
    public AfbExtractor(AfbExtractRequest request, IMediaTool mediaTool)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mediaTool);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutFolder);

        MediaTool = mediaTool;
        InPath = request.InPath;
        OutFolder = request.OutFolder;
    }

    private IMediaTool MediaTool { get; }
    private IDiagnosticSink Diagnostic { get; } = new Diagnoster();
    private string InPath { get; }
    private string OutFolder { get; }

    public async Task<OperationResult> ExtractAsync(CancellationToken ct = default)
    {
        if (!Validate()) return OperationResult.Failure().WithDiagnostics(DiagnosticSnapshot.Create(Diagnostic));

        await MediaTool.ExtractDdsAsync(InPath, OutFolder, ct);
        ct.ThrowIfCancellationRequested();
        return OperationResult.Success().WithDiagnostics(DiagnosticSnapshot.Create(Diagnostic));
    }

    private bool Validate()
    {
        if (File.Exists(InPath)) return true;

        Diagnostic.Report(Severity.Error, Strings.Error_File_not_found, InPath);
        return false;
    }
}
