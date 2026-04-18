using PenguinTools.Core.Asset;
using PenguinTools.Core.Resources;
using PenguinTools.Core.Xml;

namespace PenguinTools.Core.Media;

public class StageConverter
{
    public StageConverter(StageBuildRequest request, IMediaTool mediaTool, IEmbeddedResourceStore resources, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mediaTool);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request.Assets);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BackgroundPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutFolder);

        MediaTool = mediaTool;
        Resources = resources;
        Context = context;
        Assets = request.Assets;
        BackgroundPath = request.BackgroundPath;
        EffectPaths = request.EffectPaths;
        StageId = request.StageId;
        OutFolder = request.OutFolder;
        NoteFieldLane = request.NoteFieldLane;
    }

    private IMediaTool MediaTool { get; }
    private IEmbeddedResourceStore Resources { get; }
    private OperationContext Context { get; }
    private Diagnoster Diagnostic => Context.Diagnostic;
    private IProgress<string>? Progress => Context.Progress;
    private AssetManager Assets { get; }
    private string BackgroundPath { get; }
    private string?[]? EffectPaths { get; }
    private int? StageId { get; }
    private string OutFolder { get; }
    private Entry NoteFieldLane { get; }

    public async Task<OperationResult<Entry>> BuildAsync(CancellationToken ct = default)
    {
        if (!await ValidateAsync(ct)) return OperationResult<Entry>.Failure();
        if (StageId is not { } stageId) return OperationResult<Entry>.Failure();

        Progress?.Report(Strings.Status_Convert_background);

        var xml = new StageXml(stageId, NoteFieldLane);
        var outputDir = await xml.SaveDirectoryAsync(OutFolder);

        var nfPath = Path.Combine(outputDir, xml.NotesFieldFile);
        var stPath = Path.Combine(outputDir, xml.BaseFile);
        var stageTemplatePath = Resources.ExtractToTemp("st_dummy.afb");
        await MediaTool.ConvertStageAsync(BackgroundPath, stageTemplatePath, stPath, EffectPaths, ct);
        await Resources.CopyToAsync("nf_dummy.afb", nfPath, ct);

        return OperationResult<Entry>.Success(xml.Name);
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
