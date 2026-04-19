using PenguinTools.Core.Resources;

namespace PenguinTools.Core.Media;

public class JacketConverter
{
    public JacketConverter(JacketConvertRequest request, IMediaTool mediaTool, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mediaTool);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutPath);

        MediaTool = mediaTool;
        ParentContext = context;
        CurrentContext = context;
        InPath = request.InPath;
        OutPath = request.OutPath;
    }

    private IMediaTool MediaTool { get; }
    private OperationContext ParentContext { get; }
    private OperationContext CurrentContext { get; set; }
    private IDiagnosticSink Diagnostic => CurrentContext.Diagnostic;
    private IProgress<string>? Progress => CurrentContext.Progress;
    private string InPath { get; }
    private string OutPath { get; }

    public async Task<OperationResult> ConvertAsync(CancellationToken ct = default)
    {
        var diagnostics = new Diagnoster
        {
            TimeCalculator = ParentContext.Diagnostic.TimeCalculator
        };
        CurrentContext = ParentContext.CreateChild(diagnostics);

        try
        {
            if (!Validate()) return OperationResult.Failure().WithDiagnostics(DiagnosticSnapshot.Create(diagnostics));

            Progress?.Report(Strings.Status_Converting_jacket);
            ct.ThrowIfCancellationRequested();
            await MediaTool.ConvertJacketAsync(InPath, OutPath, ct);
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

        Diagnostic.Report(Severity.Error, Strings.Error_Jacket_file_not_found, InPath);
        return false;
    }
}
