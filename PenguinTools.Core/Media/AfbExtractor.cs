using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Media;

public class AfbExtractor
{
    public AfbExtractor(AfbExtractRequest request, IMediaTool mediaTool, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mediaTool);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutFolder);

        MediaTool = mediaTool;
        Context = context;
        InPath = request.InPath;
        OutFolder = request.OutFolder;
    }

    private IMediaTool MediaTool { get; }
    private OperationContext Context { get; }
    private Diagnoster Diagnostic => Context.Diagnostic;
    private IProgress<string>? Progress => Context.Progress;
    private string InPath { get; }
    private string OutFolder { get; }

    public async Task<OperationResult> ExtractAsync(CancellationToken ct = default)
    {
        if (!Validate()) return OperationResult.Failure();

        Progress?.Report(Strings.Status_Extracting);
        await MediaTool.ExtractDdsAsync(InPath, OutFolder, ct);
        ct.ThrowIfCancellationRequested();
        Progress?.Report(Strings.Status_Writing);
        return OperationResult.Success();
    }

    private bool Validate()
    {
        if (File.Exists(InPath)) return true;

        Diagnostic.Report(Severity.Error, Strings.Error_File_not_found, InPath);
        return false;
    }
}
