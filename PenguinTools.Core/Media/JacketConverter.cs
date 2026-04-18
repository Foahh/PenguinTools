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
        Context = context;
        InPath = request.InPath;
        OutPath = request.OutPath;
    }

    private IMediaTool MediaTool { get; }
    private OperationContext Context { get; }
    private Diagnoster Diagnostic => Context.Diagnostic;
    private IProgress<string>? Progress => Context.Progress;
    private string InPath { get; }
    private string OutPath { get; }

    public async Task<OperationResult> ConvertAsync(CancellationToken ct = default)
    {
        if (!Validate()) return OperationResult.Failure();

        Progress?.Report(Strings.Status_Converting_jacket);
        ct.ThrowIfCancellationRequested();
        await MediaTool.ConvertJacketAsync(InPath, OutPath, ct);
        ct.ThrowIfCancellationRequested();
        return OperationResult.Success();
    }

    private bool Validate()
    {
        if (File.Exists(InPath)) return true;

        Diagnostic.Report(Severity.Error, Strings.Error_Jacket_file_not_found, InPath);
        return false;
    }
}
