using PenguinTools.Chart.Resources;
using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Media;
using System.Text;

namespace PenguinTools.Chart.Parser;

using mg = Models.mgxc;

public partial class UgcParser
{
    static UgcParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public UgcParser(UgcParseRequest request, IMediaTool mediaTool)
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
    private mg.Chart Ugc { get; } = new();

#pragma warning disable CS0169, CS0414
    private int _currentTimeline;
    private mg.Note? _lastNote;
    private mg.Note? _lastParentNote;
    private readonly Dictionary<int, int> _barToTick = new();
#pragma warning restore CS0169, CS0414

    public async Task<OperationResult<mg.Chart>> ParseAsync(CancellationToken ct = default)
    {
        Ugc.Meta.FilePath = Path;
        var lines = await ReadLinesAsync(Path, ct);

        foreach (var line in lines)
        {
            ct.ThrowIfCancellationRequested();
            if (line.StartsWith('@')) DispatchHeaderLine(line);
        }

        BuildBarAxis();

        _currentTimeline = 0;
        foreach (var line in lines)
        {
            ct.ThrowIfCancellationRequested();
            if (line.StartsWith("@USETIL", StringComparison.Ordinal)) ApplyUseTil(line);
            else if (line.StartsWith('#')) DispatchBodyLine(line);
        }

        var post = new ChartPostProcessor(Ugc, Diagnostic, Assets);
        post.Run();
        ProcessMeta();

        await Task.WhenAll(Tasks);
        return OperationResult<mg.Chart>.Success(Ugc).WithDiagnostics(DiagnosticSnapshot.Create(Diagnostic));
    }

    private static async Task<string[]> ReadLinesAsync(string path, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(path, ct);
        string text;
        try
        {
            text = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            text = Encoding.GetEncoding(932).GetString(bytes);
        }

        return text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }

    private void DispatchHeaderLine(string line) { }

    private void DispatchBodyLine(string line) { }

    private void BuildBarAxis() { }

    private void ApplyUseTil(string line) { }

    private void ProcessMeta()
    {
        if (string.IsNullOrWhiteSpace(Ugc.Meta.SortName))
        {
            Ugc.Meta.SortName = ChartPostProcessor.GetSortName(Ugc.Meta.Title);
            Diagnostic.Report(Severity.Information, Strings.Mg_No_sortname_provided);
        }

        if (Ugc.Meta.IsCustomStage && !string.IsNullOrWhiteSpace(Ugc.Meta.FullBgiFilePath))
        {
            QueueValidation(
                MediaTool.CheckImageValidAsync(Ugc.Meta.FullBgiFilePath),
                Ugc.Meta.FullBgiFilePath,
                Strings.Error_Invalid_bg_image,
                () =>
                {
                    Ugc.Meta.IsCustomStage = false;
                    Ugc.Meta.BgiFilePath = string.Empty;
                });
        }
    }

    private void QueueValidation(Task<ProcessCommandResult> validationTask, string path, string message, Action onFailure)
    {
        Tasks.Add(HandleValidationAsync(validationTask, path, message, onFailure));
    }

    private async Task HandleValidationAsync(Task<ProcessCommandResult> validationTask, string path, string message, Action onFailure)
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
