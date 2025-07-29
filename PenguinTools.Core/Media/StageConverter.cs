using PenguinTools.Core.Asset;
using PenguinTools.Core.Resources;
using PenguinTools.Core.Xml;

namespace PenguinTools.Core.Media;

public class StageConverter(IDiagnostic diag, IProgress<string>? prog = null) : ConverterBase<Entry>(diag, prog)
{
    public required AssetManager Assets { get; init; }
    public required string BackgroundPath { get; init; }
    public required string?[]? EffectPaths { get; init; }
    public required int? StageId { get; init; }
    public required string OutFolder { get; init; }
    public required Entry NoteFieldLane { get; init; }

    protected async override Task<Entry> ActionAsync(CancellationToken ct = default)
    {
        if (StageId is not { } stageId) throw new DiagnosticException(Strings.Error_Stage_id_is_not_set);
        Progress?.Report(Strings.Status_Convert_background);

        var xml = new StageXml(stageId, NoteFieldLane);
        var outputDir = await xml.SaveDirectoryAsync(OutFolder);

        var nfPath = Path.Combine(outputDir, xml.NotesFieldFile);
        var stPath = Path.Combine(outputDir, xml.BaseFile);
        await Manipulate.ConvertStageAsync(BackgroundPath, ResourceUtils.GetTempPath("st_dummy.afb"), stPath, EffectPaths, ct);
        await ResourceUtils.CopyAsync("nf_dummy.afb", nfPath);

        return xml.Name;
    }

    protected async override Task ValidateAsync(CancellationToken ct = default)
    {
        var duplicates = Assets.StageNames.Where(p => p.Id == StageId);
        foreach (var d in duplicates) Diagnostic.Report(Severity.Warning, string.Format(Strings.Warn_Stage_already_exists, d, StageId));

        if (StageId is null) Diagnostic.Report(Severity.Error, string.Format(Strings.Error_Stage_id_is_not_set));
        if (!File.Exists(BackgroundPath)) Diagnostic.Report(Severity.Error, Strings.Error_File_not_found, BackgroundPath);

        var ret = await Manipulate.IsImageValidAsync(BackgroundPath, ct);
        if (ret.IsFailure) Diagnostic.Report(Severity.Error, Strings.Error_Invalid_bg_image, BackgroundPath, target: ret);
        if (EffectPaths is not null)
        {
            foreach (var p in EffectPaths)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                if (!File.Exists(p)) Diagnostic.Report(Severity.Error, Strings.Error_File_not_found, p);
                ret = await Manipulate.IsImageValidAsync(p, ct);
                if (ret.IsFailure) Diagnostic.Report(Severity.Error, Strings.Error_Invalid_bg_fx_image, p, target: ret);
            }
        }
    }
}