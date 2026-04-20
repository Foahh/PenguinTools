using PenguinTools.Core;
using PenguinTools.Core.Asset;
using PenguinTools.Core.Diagnostic;
using PenguinTools.Core.Xml;
using PenguinTools.Media.Resources;

namespace PenguinTools.Media;

public class StageConverter
{
    public StageConverter(StageBuildRequest request, IMediaTool mediaTool)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mediaTool);
        ArgumentNullException.ThrowIfNull(request.Assets);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BackgroundPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.StageTemplatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.NotesFieldTemplatePath);

        MediaTool = mediaTool;
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
    private IDiagnosticSink Diagnostic { get; } = new DiagnosticCollector();
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
        if (!await ValidateAsync(ct))
            return OperationResult<Entry>.Failure().WithDiagnostics(Diagnostic);
        if (StageId is not { } stageId)
            return OperationResult<Entry>.Failure().WithDiagnostics(Diagnostic);

        var xml = new StageXml(stageId, NoteFieldLane);
        var outputDir = await xml.SaveDirectoryAsync(OutFolder);

        var nfPath = Path.Combine(outputDir, xml.NotesFieldFile);
        var stPath = Path.Combine(outputDir, xml.BaseFile);
        await MediaTool.ConvertStageAsync(BackgroundPath, StageTemplatePath, stPath, EffectPaths, ct);
        File.Copy(NotesFieldTemplatePath, nfPath, true);

        return OperationResult<Entry>.Success(xml.Name).WithDiagnostics(Diagnostic);
    }

    private async Task<bool> ValidateAsync(CancellationToken ct = default)
    {
        var hasError = false;
        var duplicates = Assets.StageNames.Where(p => p.Id == StageId);
        foreach (var d in duplicates)
            Diagnostic.Report(new Diagnostic(Severity.Warning,
                string.Format(Strings.Warn_Stage_already_exists, d, StageId)));

        if (StageId is null)
        {
            Diagnostic.Report(new Diagnostic(Severity.Error, string.Format(Strings.Error_Stage_id_is_not_set)));
            hasError = true;
        }

        if (!File.Exists(StageTemplatePath))
        {
            Diagnostic.Report(new PathDiagnostic(Severity.Error, Strings.Error_File_not_found, StageTemplatePath));
            hasError = true;
        }

        if (!File.Exists(NotesFieldTemplatePath))
        {
            Diagnostic.Report(new PathDiagnostic(Severity.Error, Strings.Error_File_not_found, NotesFieldTemplatePath));
            hasError = true;
        }

        if (!File.Exists(BackgroundPath))
        {
            Diagnostic.Report(new PathDiagnostic(Severity.Error, Strings.Error_Background_file_not_found,
                BackgroundPath));
            hasError = true;
        }
        else
        {
            var ret = await MediaTool.CheckImageValidAsync(BackgroundPath, ct);
            if (ret.IsFailure)
            {
                Diagnostic.Report(new PathDiagnostic(Severity.Error, Strings.Error_Invalid_bg_image, BackgroundPath)
                {
                    Target = ret
                });
                hasError = true;
            }
        }

        if (EffectPaths is not null)
            foreach (var p in EffectPaths)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;

                if (!File.Exists(p))
                {
                    Diagnostic.Report(new PathDiagnostic(Severity.Error, Strings.Error_Effect_file_not_found, p));
                    hasError = true;
                    continue;
                }

                var ret = await MediaTool.CheckImageValidAsync(p, ct);
                if (ret.IsFailure)
                {
                    Diagnostic.Report(new PathDiagnostic(Severity.Error, Strings.Error_Invalid_bg_fx_image, p)
                    {
                        Target = ret
                    });
                    hasError = true;
                }
            }

        return !hasError;
    }
}
