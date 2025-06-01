using PenguinTools.Common.Metadata;
using PenguinTools.Common.Resources;
using PenguinTools.Common.Xml;

namespace PenguinTools.Common.Audio;

public sealed class MusicConverter : IConverter<MusicConverter.Context>
{
    private CriwareConverter CriwareConverter { get; } = new();

    public Task<bool> CanConvertAsync(Context context, IDiagnostic diag)
    {
        if (context.Meta.Id is null) diag.Report(Severity.Error, Strings.Error_song_id_is_not_set);
        if (context.Meta.BgmPreviewStop < context.Meta.BgmPreviewStart) diag.Report(Severity.Error, Strings.Error_preview_stop_greater_than_start);
        var path = context.Meta.FullBgmFilePath;
        if (!File.Exists(path)) diag.Report(Severity.Error, Strings.Error_file_not_found, path);
        if (context.Meta.BgmAnalysis == null) diag.Report(Severity.Error, Strings.Error_invalid_audio, path);
        return Task.FromResult(!diag.HasErrors);
    }

    public async Task ConvertAsync(Context context, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!await CanConvertAsync(context, diag)) return;
        progress?.Report(Strings.Status_converting_audio);
        if (context.Meta.BgmPreviewStart > 120) diag.Report(Severity.Warning, Strings.Diag_pv_laterthan_120);

        var realPath = await FFmpeg.ConvertAudio(context.Meta, progress, ct);

        ct.ThrowIfCancellationRequested();

        progress?.Report(Strings.Status_Convert_cue_file);

        var songId = context.Meta.Id ?? throw new DiagnosticException(Strings.Error_song_id_is_not_set);
        var xml = new CueFileXml(songId);
        var outputDir = await xml.SaveDirectoryAsync(context.OutputFolder);

        var pvStart = (double)context.Meta.BgmPreviewStart;
        var pvStop = (double)context.Meta.BgmPreviewStop;
        var acbPath = Path.Combine(outputDir, xml.AcbFile);
        var awbPath = Path.Combine(outputDir, xml.AwbFile);
        await CriwareConverter.CreateAsync(realPath, xml.DataName, acbPath, awbPath, pvStart, pvStop, ct);

        ct.ThrowIfCancellationRequested();
    }


    public record Context(Meta Meta, string OutputFolder);
}