using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using PenguinTools.Core.Metadata;
using PenguinTools.Core.Media.Resources;
using PenguinTools.Core.Xml;
using SonicAudioLib.Archives;
using SonicAudioLib.CriMw;
using VGAudio.Codecs.CriHca;
using VGAudio.Containers.Hca;
using VGAudio.Containers.Wave;

namespace PenguinTools.Core.Media;

public class MusicConverter
{
    private const ulong Key = 32931609366120192UL;

    public MusicConverter(MusicConvertRequest request, IMediaTool mediaTool)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mediaTool);
        ArgumentNullException.ThrowIfNull(request.Meta);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DummyAcbPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WorkingAudioPath);

        MediaTool = mediaTool;
        Meta = request.Meta;
        OutFolder = request.OutFolder;
        DummyAcbPath = request.DummyAcbPath;
        WorkingAudioPath = request.WorkingAudioPath;
    }

    private IMediaTool MediaTool { get; }
    private IDiagnosticSink Diagnostic { get; } = new Diagnoster();
    private Meta Meta { get; }
    private string OutFolder { get; }
    private string DummyAcbPath { get; }
    private string WorkingAudioPath { get; }

    public async Task<OperationResult> ConvertAsync(CancellationToken ct = default)
    {
        if (!Validate()) return OperationResult.Failure().WithDiagnostics(DiagnosticSnapshot.Create(Diagnostic));

        var songId = Meta.Id ?? throw new DiagnosticException(Strings.Error_Song_id_is_not_set);

        if (Meta.BgmPreviewStart > 120) { Diagnostic.Report(Severity.Warning, Strings.Warn_Preview_later_than_120); }

        var srcPath = Meta.FullBgmFilePath;
        var wavPath = WorkingAudioPath;

        var ret = await MediaTool.NormalizeAudioAsync(srcPath, wavPath, Meta.BgmRealOffset, ct);
        if (ret.IsNoOperation) { wavPath = srcPath; }

        ct.ThrowIfCancellationRequested();

        var xml = new CueFileXml(songId);
        var outputDir = await xml.SaveDirectoryAsync(OutFolder);

        var pvStart = Meta.BgmPreviewStart;
        var pvStop = Meta.BgmPreviewStop;
        if (Meta.BgmEnableBarOffset)
        {
            pvStart += Meta.BgmBarOffset;
            pvStop += Meta.BgmBarOffset;
        }

        var maxSeconds = Math.Floor(uint.MaxValue / 1000m);
        var originalPvStart = pvStart;
        var originalPvStop = pvStop;
        pvStart = Math.Clamp(pvStart, 0, maxSeconds);
        pvStop = Math.Clamp(pvStop, 0, maxSeconds);

        if (originalPvStart > maxSeconds)
        {
            var msg = string.Format(Strings.Hint_Preview_value_clamped, nameof(Meta.BgmPreviewStart), originalPvStart, maxSeconds);
            Diagnostic.Report(Severity.Information, msg);
        }
        if (originalPvStop > maxSeconds)
        {
            var msg = string.Format(Strings.Hint_Preview_value_clamped, nameof(Meta.BgmPreviewStop), originalPvStop, maxSeconds);
            Diagnostic.Report(Severity.Information, msg);
        }

        var acbPath = Path.Combine(outputDir, xml.AcbFile);
        var awbPath = Path.Combine(outputDir, xml.AwbFile);

        // Criware Build
        // Credits to Margrithm (https://margrithm.girlsband.party/)

        var waveReader = new WaveReader();

        var wav = waveReader.ReadFormat(wavPath);
        if (wav.ChannelCount != 2 || wav.SampleRate != 48000)
        {
            throw new DiagnosticException(Strings.Error_Audio_format_not_supported);
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

        using var dummyAcb = File.OpenRead(DummyAcbPath);
        cueSheetTable.Load(dummyAcb);
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
        return OperationResult.Success().WithDiagnostics(DiagnosticSnapshot.Create(Diagnostic));
    }

    private bool Validate()
    {
        var hasError = false;
        if (Meta.Id is null)
        {
            Diagnostic.Report(Severity.Error, Strings.Error_Song_id_is_not_set);
            hasError = true;
        }

        if (Meta.BgmPreviewStop < Meta.BgmPreviewStart)
        {
            Diagnostic.Report(Severity.Error, Strings.Error_Preview_stop_greater_than_start);
            hasError = true;
        }

        var path = Meta.FullBgmFilePath;
        if (!File.Exists(path))
        {
            Diagnostic.Report(Severity.Error, Strings.Error_Audio_file_not_found, path);
            hasError = true;
        }

        if (!File.Exists(DummyAcbPath))
        {
            Diagnostic.Report(Severity.Error, Strings.Error_File_not_found, DummyAcbPath);
            hasError = true;
        }

        return !hasError;
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
