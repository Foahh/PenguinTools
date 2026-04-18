using PenguinTools.Core.Asset;
using PenguinTools.Core.Resources;
using PenguinTools.Core.Xml;

namespace PenguinTools.Core.Media;

public class StageConverter
{
    public StageConverter(StageBuildRequest request, IMediaTool mediaTool, IEmbeddedResourceStore resources, Diagnoster diag, IProgress<string>? prog = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mediaTool);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(diag);
        ArgumentNullException.ThrowIfNull(request.Assets);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BackgroundPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutFolder);

        MediaTool = mediaTool;
        Resources = resources;
        Diagnostic = diag;
        Progress = prog;
        Assets = request.Assets;
        BackgroundPath = request.BackgroundPath;
        EffectPaths = request.EffectPaths;
        StageId = request.StageId;
        OutFolder = request.OutFolder;
        NoteFieldLane = request.NoteFieldLane;
    }

    private IMediaTool MediaTool { get; }
    private IEmbeddedResourceStore Resources { get; }
    private Diagnoster Diagnostic { get; }
    private IProgress<string>? Progress { get; }
    private AssetManager Assets { get; }
    private string BackgroundPath { get; }
    private string?[]? EffectPaths { get; }
    private int? StageId { get; }
    private string OutFolder { get; }
    private Entry NoteFieldLane { get; }

    public async Task<Entry?> BuildAsync(CancellationToken ct = default)
    {
        if (!await ValidateAsync(ct)) return null;
        if (StageId is not { } stageId) return null;

        Progress?.Report(Strings.Status_Convert_background);

        var xml = new StageXml(stageId, NoteFieldLane);
        var outputDir = await xml.SaveDirectoryAsync(OutFolder);

        var nfPath = Path.Combine(outputDir, xml.NotesFieldFile);
        var stPath = Path.Combine(outputDir, xml.BaseFile);
        var stageTemplatePath = Resources.ExtractToTemp("st_dummy.afb");
        await MediaTool.ConvertStageAsync(BackgroundPath, stageTemplatePath, stPath, EffectPaths, ct);
        await Resources.CopyToAsync("nf_dummy.afb", nfPath, ct);

        return xml.Name;
    }

    private async Task<bool> ValidateAsync(CancellationToken ct = default)
    {
        var duplicates = Assets.StageNames.Where(p => p.Id == StageId);
        foreach (var d in duplicates)
        {
            Diagnostic.Report(Severity.Warning, string.Format(Strings.Warn_Stage_already_exists, d, StageId));
        }

        if (StageId is null) { Diagnostic.Report(Severity.Error, string.Format(Strings.Error_Stage_id_is_not_set)); }

        if (!File.Exists(BackgroundPath))
        {
            Diagnostic.Report(Severity.Error, Strings.Error_Background_file_not_found, BackgroundPath);
        }
        else
        {
            var ret = await MediaTool.CheckImageValidAsync(BackgroundPath, ct);
            if (ret.IsFailure) { Diagnostic.Report(Severity.Error, Strings.Error_Invalid_bg_image, BackgroundPath, ret); }
        }

        if (EffectPaths is not null)
        {
            foreach (var p in EffectPaths)
            {
                if (string.IsNullOrWhiteSpace(p)) { continue; }

                if (!File.Exists(p))
                {
                    Diagnostic.Report(Severity.Error, Strings.Error_Effect_file_not_found, p);
                    continue;
                }

                var ret = await MediaTool.CheckImageValidAsync(p, ct);
                if (ret.IsFailure) { Diagnostic.Report(Severity.Error, Strings.Error_Invalid_bg_fx_image, p, ret); }
            }
        }

        return !Diagnostic.HasError;
    }
}
