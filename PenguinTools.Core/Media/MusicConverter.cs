using PenguinTools.Core.Metadata;
using PenguinTools.Core.Resources;
using PenguinTools.Core.Xml;
using SonicAudioLib.Archives;
using SonicAudioLib.CriMw;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using VGAudio.Codecs.CriHca;
using VGAudio.Containers.Hca;
using VGAudio.Containers.Wave;

namespace PenguinTools.Core.Media;

public class MusicConverter : IConverter<MusicConverter.Context>
{
    public ulong Key { get; set; } = 32931609366120192UL;

    public Task<bool> CanConvertAsync(Context context, IDiagnostic diag)
    {
        if (context.Meta.Id is null) diag.Report(Severity.Error, Strings.Error_song_id_is_not_set);
        if (context.Meta.BgmPreviewStop < context.Meta.BgmPreviewStart) diag.Report(Severity.Error, Strings.Error_preview_stop_greater_than_start);
        var path = context.Meta.FullBgmFilePath;
        if (!File.Exists(path)) diag.Report(Severity.Error, Strings.Error_file_not_found, path);
        return Task.FromResult(!diag.HasError);
    }

    public async Task ConvertAsync(Context ctx, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!await CanConvertAsync(ctx, diag)) return;
        var meta = ctx.Meta;
        var songId = meta.Id ?? throw new DiagnosticException(Strings.Error_song_id_is_not_set);

        progress?.Report(Strings.Status_converting_audio);
        if (meta.BgmPreviewStart > 120) diag.Report(Severity.Warning, Strings.Diag_pv_laterthan_120);
        
        var srcPath = meta.FullBgmFilePath;
        var wavPath = ResourceUtils.GetTempPath($"c_{Path.GetFileNameWithoutExtension(srcPath)}.wav");

        var ret = await Manipulate.NormalizeAsync(srcPath, wavPath, meta.BgmRealOffset, ct);
        if (ret.IsNoOperation) wavPath = srcPath;

        ct.ThrowIfCancellationRequested();

        var xml = new CueFileXml(songId);
        var outputDir = await xml.SaveDirectoryAsync(ctx.DestinationFolder);

        var pvStart = (double)meta.BgmPreviewStart;
        var pvStop = (double)meta.BgmPreviewStop;
        var acbPath = Path.Combine(outputDir, xml.AcbFile);
        var awbPath = Path.Combine(outputDir, xml.AwbFile);

        // Criware Build
        // Credits to Margrithm (https://margrithm.girlsband.party/)
        
        var waveReader = new WaveReader();

        var wav = waveReader.ReadFormat(wavPath);
        if (wav.ChannelCount != 2 || wav.SampleRate != 48000)
        {
            throw new DiagnosticException(Strings.Error_audio_format_not_supported);
        }
        
        ct.ThrowIfCancellationRequested();

        var hcaWriter = new HcaWriter();
        var config = new HcaConfiguration
        {
            Bitrate = 16384 * 8,
            Quality = CriHcaQuality.Highest,
            TrimFile = false,
            EncryptionKey = new CriHcaKey(Key)
        };

        await using var hcaStream = new MemoryStream();
        hcaWriter.WriteToStream(wav, hcaStream, config);
        hcaStream.Seek(0, SeekOrigin.Begin);
        
        ct.ThrowIfCancellationRequested();
        
        var cueSheetTable = new CriTable();

        cueSheetTable.Load(ResourceUtils.GetStream("dummy.acb"));
        cueSheetTable.Rows[0]["Name"] = xml.DataName;

        var cueTable = new CriTable();
        cueTable.Load(cueSheetTable.Rows[0]["CueTable"] as byte[]);

        var lengthMs = (int)Math.Round(wav.SampleCount * 1000.0 / wav.SampleRate);
        cueTable.Rows[0]["Length"] = lengthMs;

        cueTable.WriterSettings = CriTableWriterSettings.Adx2Settings;
        cueSheetTable.Rows[0]["CueTable"] = cueTable.Save();

        var trackEventTable = new CriTable();
        trackEventTable.Load(cueSheetTable.Rows[0]["TrackEventTable"] as byte[]);

        var cmdData = trackEventTable.Rows[1]["Command"] as byte[];
        var cmdStream = new MemoryStream(cmdData!);
        await using (var bw = new BinaryWriter(cmdStream, Encoding.Default, true))
        {
            cmdStream.Position = 3;
            bw.WriteUInt32BigEndian((uint)(pvStart * 1000.0));
            cmdStream.Position = 17;
            bw.WriteUInt32BigEndian((uint)(pvStop * 1000.0));
        }
        trackEventTable.Rows[1]["Command"] = cmdStream.ToArray();
        cueSheetTable.Rows[0]["TrackEventTable"] = trackEventTable.Save();

        var awbEntry = new CriAfs2Entry { Stream = hcaStream };
        var awbArchive = new CriAfs2Archive { awbEntry };
        await using var awbStream = File.Create(awbPath);
        awbArchive.Save(awbStream);
        awbStream.Position = 0;

        var streamAwbHashTbl = new CriTable();
        streamAwbHashTbl.Load(cueSheetTable.Rows[0]["StreamAwbHash"] as byte[]);

        var sha = await SHA1.HashDataAsync(awbStream, ct);
        streamAwbHashTbl.Rows[0]["Name"] = xml.DataName;
        streamAwbHashTbl.Rows[0]["Hash"] = sha;
        cueSheetTable.Rows[0]["StreamAwbHash"] = streamAwbHashTbl.Save();

        var waveformTable = new CriTable();
        waveformTable.Load(cueSheetTable.Rows[0]["WaveformTable"] as byte[]);

        waveformTable.Rows[0]["SamplingRate"] = (ushort)wav.SampleRate;
        waveformTable.Rows[0]["NumSamples"] = wav.SampleCount;
        cueSheetTable.Rows[0]["WaveformTable"] = waveformTable.Save();

        cueSheetTable.WriterSettings = CriTableWriterSettings.Adx2Settings;
        await using var acbStream = File.Create(acbPath);
        cueSheetTable.Save(acbStream);
    }

    public record Context(Meta Meta, string DestinationFolder);
}

file static class BinaryWriterExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt32BigEndian(this BinaryWriter bw, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        bw.Write(buffer);
    }
}