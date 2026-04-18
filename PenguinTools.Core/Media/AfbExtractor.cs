using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Media;

public class AfbExtractor
{
    public AfbExtractor(AfbExtractRequest request, IMediaTool mediaTool, Diagnoster diag, IProgress<string>? prog = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mediaTool);
        ArgumentNullException.ThrowIfNull(diag);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutFolder);

        MediaTool = mediaTool;
        Diagnostic = diag;
        Progress = prog;
        InPath = request.InPath;
        OutFolder = request.OutFolder;
    }

    private IMediaTool MediaTool { get; }
    private Diagnoster Diagnostic { get; }
    private IProgress<string>? Progress { get; }
    private string InPath { get; }
    private string OutFolder { get; }

    public async Task<bool> ExtractAsync(CancellationToken ct = default)
    {
        if (!Validate()) return false;

        Progress?.Report(Strings.Status_Extracting);
        await MediaTool.ExtractDdsAsync(InPath, OutFolder, ct);
        ct.ThrowIfCancellationRequested();
        Progress?.Report(Strings.Status_Writing);
        return !Diagnostic.HasError;
    }

    private bool Validate()
    {
        if (!File.Exists(InPath)) Diagnostic.Report(Severity.Error, Strings.Error_File_not_found, InPath);
        return !Diagnostic.HasError;
    }
}
