using PenguinTools.Common.Resources;
using SonicAudioLib.Archives;
using SonicAudioLib.CriMw;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using VGAudio.Codecs.CriHca;
using VGAudio.Containers.Hca;
using VGAudio.Containers.Wave;

/*
 * Originally by Margrithm
 * https://margrithm.girlsband.party/
 */

namespace PenguinTools.Common.Audio;

public class CriwareConverter
{
    private static Stream DummyAcb => ResourceManager.GetStream("dummy.acb");

    public ulong Key { get; set; } = 32931609366120192UL;

    public async Task CreateAsync(string wavePath, string cueName, string acbPath, string awbPath, double loopStart, double loopEnd, CancellationToken ct = default)
    {
        var waveReader = new WaveReader();

        var data = waveReader.ReadFormat(wavePath);
        if (data.ChannelCount != 2 || data.SampleRate != 48000)
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

        await using var hcaMs = new MemoryStream();
        hcaWriter.WriteToStream(data, hcaMs, config);
        hcaMs.Seek(0, SeekOrigin.Begin);

        var cueSheetTable = new CriTable();

        cueSheetTable.Load(DummyAcb);
        cueSheetTable.Rows[0]["Name"] = cueName;

        var cueTable = new CriTable();
        cueTable.Load(cueSheetTable.Rows[0]["CueTable"] as byte[]);

        var lengthMs = (int)(data.SampleCount / (double)data.SampleRate * 1000.0);
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
            bw.WriteUInt32BigEndian((uint)(loopStart * 1000.0));
            cmdStream.Position = 17;
            bw.WriteUInt32BigEndian((uint)(loopEnd * 1000.0));
        }
        trackEventTable.Rows[1]["Command"] = cmdStream.ToArray();
        cueSheetTable.Rows[0]["TrackEventTable"] = trackEventTable.Save();

        var awbEntry = new CriAfs2Entry { Stream = hcaMs };
        var awbArchive = new CriAfs2Archive { awbEntry };
        await using var awbStream = File.Create(awbPath);
        awbArchive.Save(awbStream);
        awbStream.Position = 0;

        var streamAwbHashTbl = new CriTable();
        streamAwbHashTbl.Load(cueSheetTable.Rows[0]["StreamAwbHash"] as byte[]);

        var sha = await SHA1.HashDataAsync(awbStream, ct);
        streamAwbHashTbl.Rows[0]["Name"] = cueName;
        streamAwbHashTbl.Rows[0]["Hash"] = sha;
        cueSheetTable.Rows[0]["StreamAwbHash"] = streamAwbHashTbl.Save();

        var waveformTable = new CriTable();
        waveformTable.Load(cueSheetTable.Rows[0]["WaveformTable"] as byte[]);

        waveformTable.Rows[0]["SamplingRate"] = (ushort)data.SampleRate;
        waveformTable.Rows[0]["NumSamples"] = data.SampleCount;
        cueSheetTable.Rows[0]["WaveformTable"] = waveformTable.Save();

        cueSheetTable.WriterSettings = CriTableWriterSettings.Adx2Settings;
        await using var acbStream = File.Create(acbPath);
        cueSheetTable.Save(acbStream);
    }
}

public static class BinaryWriterExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt32BigEndian(this BinaryWriter bw, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        bw.Write(buffer);
    }
}