using PenguinTools.Core.Asset;
using PenguinTools.Core.Resources;
using PenguinTools.Core.Xml;

namespace PenguinTools.Core.Media;

public class StageConverter
{
    public StageConverter(StageBuildRequest request, IMediaTool mediaTool, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mediaTool);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request.Assets);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BackgroundPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.StageTemplatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.NotesFieldTemplatePath);

        MediaTool = mediaTool;
        ParentContext = context;
        CurrentContext = context;
        Assets = request.Assets;
        BackgroundPath = request.BackgroundPath;
        EffectPaths = request.EffectPaths;
        StageId = request.StageId;
        OutFolder = request.OutFolder;
        NoteFieldLane = request.NoteFieldLane;
        StageTemplatePath = request.StageTemplatePath;
        NotesFieldTemplatePath = request.NotesFieldTemplatePath;
    }

    private IMediaTool MediaTool { get; }
    private OperationContext ParentContext { get; }
    private OperationContext CurrentContext { get; set; }
    private IDiagnosticSink Diagnostic => CurrentContext.Diagnostic;
    private IProgress<string>? Progress => CurrentContext.Progress;
    private AssetManager Assets { get; }
    private string BackgroundPath { get; }
    private string?[]? EffectPaths { get; }
    private int? StageId { get; }
    private string OutFolder { get; }
    private Entry NoteFieldLane { get; }
    private string StageTemplatePath { get; }
    private string NotesFieldTemplatePath { get; }

    public async Task<OperationResult<Entry>> BuildAsync(CancellationToken ct = default)
    {
        var diagnostics = new Diagnoster
        {
            TimeCalculator = ParentContext.Diagnostic.TimeCalculator
        };
        CurrentContext = ParentContext.CreateChild(diagnostics);

        try
        {
            if (!await ValidateAsync(ct)) return OperationResult<Entry>.Failure().WithDiagnostics(DiagnosticSnapshot.Create(diagnostics));
            if (StageId is not { } stageId) return OperationResult<Entry>.Failure().WithDiagnostics(DiagnosticSnapshot.Create(diagnostics));

            Progress?.Report(Strings.Status_Convert_background);

            var xml = new StageXml(stageId, NoteFieldLane);
            var outputDir = await xml.SaveDirectoryAsync(OutFolder);

            var nfPath = Path.Combine(outputDir, xml.NotesFieldFile);
            var stPath = Path.Combine(outputDir, xml.BaseFile);
            await MediaTool.ConvertStageAsync(BackgroundPath, StageTemplatePath, stPath, EffectPaths, ct);
            File.Copy(NotesFieldTemplatePath, nfPath, true);

            return OperationResult<Entry>.Success(xml.Name).WithDiagnostics(DiagnosticSnapshot.Create(diagnostics));
        }
        finally
        {
            CurrentContext = ParentContext;
        }
    }

    private async Task<bool> ValidateAsync(CancellationToken ct = default)
    {
        var hasError = false;
        var duplicates = Assets.StageNames.Where(p => p.Id == StageId);
        foreach (var d in duplicates)
        {
            Diagnostic.Report(Severity.Warning, string.Format(Strings.Warn_Stage_already_exists, d, StageId));
        }

        if (StageId is null)
        {
            Diagnostic.Report(Severity.Error, string.Format(Strings.Error_Stage_id_is_not_set));
            hasError = true;
        }

        if (!File.Exists(StageTemplatePath))
        {
            Diagnostic.Report(Severity.Error, Strings.Error_File_not_found, StageTemplatePath);
            hasError = true;
        }

        if (!File.Exists(NotesFieldTemplatePath))
        {
            Diagnostic.Report(Severity.Error, Strings.Error_File_not_found, NotesFieldTemplatePath);
            hasError = true;
        }

        if (!File.Exists(BackgroundPath))
        {
            Diagnostic.Report(Severity.Error, Strings.Error_Background_file_not_found, BackgroundPath);
            hasError = true;
        }
        else
        {
            var ret = await MediaTool.CheckImageValidAsync(BackgroundPath, ct);
            if (ret.IsFailure)
            {
                Diagnostic.Report(Severity.Error, Strings.Error_Invalid_bg_image, BackgroundPath, ret);
                hasError = true;
            }
        }

        if (EffectPaths is not null)
        {
            foreach (var p in EffectPaths)
            {
                if (string.IsNullOrWhiteSpace(p)) { continue; }

                if (!File.Exists(p))
                {
                    Diagnostic.Report(Severity.Error, Strings.Error_Effect_file_not_found, p);
                    hasError = true;
                    continue;
                }

                var ret = await MediaTool.CheckImageValidAsync(p, ct);
                if (ret.IsFailure)
                {
                    Diagnostic.Report(Severity.Error, Strings.Error_Invalid_bg_fx_image, p, ret);
                    hasError = true;
                }
            }
        }

        return !hasError;
    }
}
