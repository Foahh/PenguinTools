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

public class MusicConverter(IDiagnostic diag, IProgress<string>? prog = null) : ConverterBase(diag, prog)
{
    public ulong Key { get; set; } = 32931609366120192UL;
    public required Meta Meta { get; init; }
    public required string OutFolder { get; init; }

    protected override Task ValidateAsync(CancellationToken ct = default)
    {
        if (Meta.Id is null) Diagnostic.Report(Severity.Error, Strings.Error_song_id_is_not_set);
        if (Meta.BgmPreviewStop < Meta.BgmPreviewStart) Diagnostic.Report(Severity.Error, Strings.Error_preview_stop_greater_than_start);
        var path = Meta.FullBgmFilePath;
        if (!File.Exists(path)) Diagnostic.Report(Severity.Error, Strings.Error_file_not_found, path);
        return Task.CompletedTask;
    }

    protected async override Task ActionAsync(CancellationToken ct = default)
    {
        var songId = Meta.Id ?? throw new DiagnosticException(Strings.Error_song_id_is_not_set);

        Progress?.Report(Strings.Status_converting_audio);
        if (Meta.BgmPreviewStart > 120) Diagnostic.Report(Severity.Warning, Strings.Diag_pv_laterthan_120);

        var srcPath = Meta.FullBgmFilePath;
        var wavPath = ResourceUtils.GetTempPath($"c_{Path.GetFileNameWithoutExtension(srcPath)}.wav");

        var ret = await Manipulate.NormalizeAsync(srcPath, wavPath, Meta.BgmRealOffset, ct);
        if (ret.IsNoOperation) wavPath = srcPath;

        ct.ThrowIfCancellationRequested();

        var xml = new CueFileXml(songId);
        var outputDir = await xml.SaveDirectoryAsync(OutFolder);

        var pvStart = Meta.BgmPreviewStart;
        var pvStop = Meta.BgmPreviewStop;
        if (Meta.BgmEnableBarOffset)
        {
            pvStart += Meta.BgmRealOffset;
            pvStop += Meta.BgmRealOffset;
        }

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
            bw.WriteUInt32BigEndian((uint)(pvStart * 1000.0m));
            cmdStream.Position = 17;
            bw.WriteUInt32BigEndian((uint)(pvStop * 1000.0m));
        }
        trackEventTable.Rows[1]["Command"] = cmdStream.ToArray();
        cueSheetTable.Rows[0]["TrackEventTable"] = trackEventTable.Save();

        var awbEntry = new CriAfs2Entry
        {
            Stream = hcaStream
        };
        var awbArchive = new CriAfs2Archive
        {
            awbEntry
        };
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