using PenguinTools.Chart.Resources;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Media;

namespace PenguinTools.Chart.Parser.mgxc;

using umgr = Models.umgr;

public partial class MgxcParser
{
    private const string HeaderMgxc = "MGXC"; // 4D 47 58 43
    private const string HeaderMeta = "meta"; // 6D 65 74 61
    private const string HeaderEvnt = "evnt"; // 65 76 6E 74
    private const string HeaderDat2 = "dat2"; // 64 61 74 32

    public MgxcParser(MgxcParseRequest request, IMediaTool mediaTool)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mediaTool);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Path);
        ArgumentNullException.ThrowIfNull(request.Assets);

        MediaTool = mediaTool;
        Path = request.Path;
        Assets = request.Assets;
    }

    private IMediaTool MediaTool { get; }
    private IDiagnosticSink Diagnostic { get; } = new Diagnoster();
    private string Path { get; }
    private AssetManager Assets { get; }
    private List<Task> Tasks { get; } = [];
    private umgr.Chart Mgxc { get; } = new();

    private void ReportAtPosition(Severity severity, string message, long position, object? target = null)
    {
        Diagnostic.Report(new Diagnostic(severity, message, Path, target: target, line: checked((int)position)));
    }

    private void ReportAtPosition(Severity severity, string message, int tick, long position, object? target = null)
    {
        Diagnostic.Report(new Diagnostic(severity, message, Path, tick, target, checked((int)position)));
    }

    private void ThrowAtPosition(string message, long position, object? target = null, int? tick = null)
    {
        throw new DiagnosticException(message, target, tick, Path, checked((int)position));
    }

    public async Task<OperationResult<umgr.Chart>> ParseAsync(CancellationToken ct = default)
    {
        try
        {
            Mgxc.Meta.FilePath = Path;

            await using var fs = File.OpenRead(Path);
            using var br = new BinaryReader(fs);

            var header = br.ReadUtf8String(4);
            if (header != HeaderMgxc)
                ThrowAtPosition(string.Format(Strings.Error_Invalid_Header, header, HeaderMgxc), fs.Position - 4);

            br.ReadInt32(); // MGXC Block Size
            br.ReadInt32(); // unknown

            br.ReadBlock(HeaderMeta, ParseMeta);

            br.ReadBlock(HeaderEvnt, ParseEvent);

            Diagnostic.TimeCalculator = Mgxc.GetCalculator();

            br.ReadBlock(HeaderDat2, ParseNote);

            var post = new ChartPostProcessor(Mgxc, Diagnostic, Assets);
            post.Run();
            ProcessMeta();

            await Task.WhenAll(Tasks);
            return OperationResult<umgr.Chart>.Success(Mgxc).WithDiagnostics(DiagnosticSnapshot.Create(Diagnostic));
        }
        catch (DiagnosticException ex)
        {
            Diagnostic.Report(ex);
            return OperationResult<umgr.Chart>.Failure().WithDiagnostics(DiagnosticSnapshot.Create(Diagnostic));
        }
    }

    private void ProcessMeta()
    {
        if (string.IsNullOrWhiteSpace(Mgxc.Meta.SortName))
        {
            Mgxc.Meta.SortName = ChartPostProcessor.GetSortName(Mgxc.Meta.Title);
            Diagnostic.Report(Severity.Information, Strings.Mg_No_sortname_provided);
        }

        if (Mgxc.Meta.IsCustomStage && !string.IsNullOrWhiteSpace(Mgxc.Meta.FullBgiFilePath))
            QueueValidation(
                MediaTool.CheckImageValidAsync(Mgxc.Meta.FullBgiFilePath),
                Mgxc.Meta.FullBgiFilePath,
                Strings.Error_Invalid_bg_image,
                () =>
                {
                    Mgxc.Meta.IsCustomStage = false;
                    Mgxc.Meta.BgiFilePath = string.Empty;
                });
    }

    private void QueueValidation(Task<ProcessCommandResult> validationTask, string path, string message,
        Action onFailure)
    {
        Tasks.Add(HandleValidationAsync(validationTask, path, message, onFailure));
    }

    private async Task HandleValidationAsync(Task<ProcessCommandResult> validationTask, string path, string message,
        Action onFailure)
    {
        try
        {
            var result = await validationTask;
            if (result.IsSuccess) return;

            onFailure();
            Diagnostic.Report(Severity.Warning, message, path, result);
        }
        catch (Exception ex)
        {
            onFailure();
            Diagnostic.Report(Severity.Warning, message, path, ex);
        }
    }
}
