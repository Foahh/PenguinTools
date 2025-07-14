using PenguinTools.Core.Asset;
using PenguinTools.Core.Resources;
using PenguinTools.Core.Xml;

namespace PenguinTools.Core.Media;

public class StageConverter(AssetManager asm) : IConverter<StageConverter.Context>
{
    public async Task ConvertAsync(Context ctx, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!await CanConvertAsync(ctx, diag)) return;
        if (ctx.StageId is not { } stageId) throw new DiagnosticException(Strings.Error_stage_id_is_not_set);

        progress?.Report(Strings.Status_processing_background);
        
        var xml = new StageXml(stageId, ctx.NoteFieldLane);
        ctx.Result = xml.Name;
        var outputDir = await xml.SaveDirectoryAsync(ctx.DestinationFolder);

        var nfPath = Path.Combine(outputDir, xml.NotesFieldFile);
        var stPath = Path.Combine(outputDir, xml.BaseFile);
        await Manipulate.ConvertStageAsync(ctx.BgPath, ResourceUtils.GetTempPath("st_dummy.afb"),stPath, ctx.FxPaths, ct);
        await ResourceUtils.CopyAsync("nf_dummy.afb", nfPath);
    }

    public async Task<bool> CanConvertAsync(Context context, IDiagnostic diag)
    {
        var duplicates = asm.StageNames.Where(p => p.Id == context.StageId);
        foreach (var d in duplicates) diag.Report(Severity.Warning, string.Format(Strings.Diag_stage_already_exists, d, context.StageId));

        if (context.StageId is null) diag.Report(Severity.Error, string.Format(Strings.Error_stage_id_is_not_set));
        if (!File.Exists(context.BgPath)) diag.Report(Severity.Error, Strings.Error_file_not_found, context.BgPath);

        var ret = await Manipulate.IsImageValidAsync(context.BgPath);
        if (ret.IsFailure) diag.Report(Severity.Error, Strings.Error_invalid_bg_image, context.BgPath, target: ret);
        if (context.FxPaths is not null)
        {
            foreach (var p in context.FxPaths)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                if (!File.Exists(p)) diag.Report(Severity.Error, Strings.Error_file_not_found, p);
                ret = await Manipulate.IsImageValidAsync(p);
                if (ret.IsFailure) diag.Report(Severity.Error, Strings.Error_invalid_bg_fx_image, p, target: ret);
            }
        }

        return !diag.HasError;
    }

    public record Context(string BgPath, string?[]? FxPaths, int? StageId, string DestinationFolder, Entry NoteFieldLane)
    {
        public Entry Result { get; set; } = Entry.Default;
    }
}