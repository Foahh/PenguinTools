using PenguinTools.Common.Asset;
using PenguinTools.Common.Resources;
using PenguinTools.Common.Xml;


namespace PenguinTools.Common.Graphic;

public sealed class StageConverter(AssetManager asm) : IConverter<StageConverter.Context>
{
    public async Task ConvertAsync(Context context, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!await CanConvertAsync(context, diag)) return;
        if (context.StageId is not { } stageId) throw new DiagnosticException(Strings.Error_stage_id_is_not_set);

        progress?.Report(Strings.Status_processing_background);

        ct.ThrowIfCancellationRequested();

        var fxPaths = context.FxPaths?.Select(p => string.IsNullOrWhiteSpace(p) ? null : p).ToArray();
        ct.ThrowIfCancellationRequested();

        var xml = new StageXml(stageId, context.NoteFieldLane);
        context.Result = xml.Name;
        var outputDir = await xml.SaveDirectoryAsync(context.OutputFolder);

        var nfPath = Path.Combine(outputDir, xml.NotesFieldFile);
        var stPath = Path.Combine(outputDir, xml.BaseFile);
        MuaInterop.ConvertStage(context.BgPath, fxPaths, stPath, nfPath);
    }

    public Task<bool> CanConvertAsync(Context context, IDiagnostic diag)
    {
        var duplicates = asm.StageNames.Where(p => p.Id == context.StageId);
        foreach (var d in duplicates) diag.Report(Severity.Warning, string.Format(Strings.Diag_stage_already_exists, d, context.StageId));

        if (context.StageId is null) diag.Report(Severity.Error, string.Format(Strings.Error_stage_id_is_not_set));
        if (!File.Exists(context.BgPath)) diag.Report(Severity.Error, Strings.Error_file_not_found, context.BgPath);

        if (!MuaInterop.IsValidImage(context.BgPath)) diag.Report(Severity.Error, Strings.Error_invalid_bg_image, context.BgPath);
        if (context.FxPaths is not null)
        {
            foreach (var p in context.FxPaths)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                if (!File.Exists(p)) diag.Report(Severity.Error, Strings.Error_file_not_found, p);
                if (!MuaInterop.IsValidImage(p)) diag.Report(Severity.Error, Strings.Error_invalid_bg_fx_image, p);
            }
        }

        return Task.FromResult(!diag.HasErrors);
    }

    public record Context(string BgPath, string?[]? FxPaths, int? StageId, string OutputFolder, Entry NoteFieldLane)
    {
        public Entry Result { get; set; } = Entry.Default;
    }
}