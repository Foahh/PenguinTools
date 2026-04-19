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
        ParentContext = context;
        CurrentContext = context;
        InPath = request.InPath;
        OutFolder = request.OutFolder;
    }

    private IMediaTool MediaTool { get; }
    private OperationContext ParentContext { get; }
    private OperationContext CurrentContext { get; set; }
    private IDiagnosticSink Diagnostic => CurrentContext.Diagnostic;
    private string InPath { get; }
    private string OutFolder { get; }

    public async Task<OperationResult> ExtractAsync(CancellationToken ct = default)
    {
        var diagnostics = new Diagnoster
        {
            TimeCalculator = ParentContext.Diagnostic.TimeCalculator
        };
        CurrentContext = ParentContext.CreateChild(diagnostics);

        try
        {
            if (!Validate()) return OperationResult.Failure().WithDiagnostics(DiagnosticSnapshot.Create(diagnostics));

            await MediaTool.ExtractDdsAsync(InPath, OutFolder, ct);
            ct.ThrowIfCancellationRequested();
            return OperationResult.Success().WithDiagnostics(DiagnosticSnapshot.Create(diagnostics));
        }
        finally
        {
            CurrentContext = ParentContext;
        }
    }

    private bool Validate()
    {
        if (File.Exists(InPath)) return true;

        Diagnostic.Report(Severity.Error, Strings.Error_File_not_found, InPath);
        return false;
    }
}
