using PenguinTools.Common.Metadata;
using PenguinTools.Common.Resources;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PenguinTools.Common.Audio;

public static partial class FFmpeg
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public async static Task<string> ConvertAudio(Meta meta, IProgress<string>? p, CancellationToken ct = default)
    {
        var analysisTask = meta.BgmAnalysis;
        if (analysisTask is null) throw new DiagnosticException(Strings.Error_invalid_audio, meta.FullBgmFilePath);
        var analysis = await analysisTask;

        var bgmPath = meta.FullBgmFilePath;
        var gain = meta.TargetTargetLoudness - analysis.IntegratedLoudness;
        var needVoluming = Math.Abs(gain) > meta.TargetGainTolerance;
        var needLimiting = meta.IsTpLimiting && analysis.TruePeak > meta.TargetMaxTruePeak;
        var needOffset = Math.Abs(meta.BgmRealOffset) > 0.000001m;
        var needReformat = analysis.SampleRate != meta.TargetSampleRate || analysis.Codec != meta.TargetCodec || analysis.ChannelCount != meta.TargetChannelCount;

        var needsConvert = needVoluming || needLimiting || needOffset || needReformat;
        if (!needsConvert) return bgmPath;

        p?.Report(Strings.Status_normalizing);

        var path = ResourceManager.GetTempPath($"c_{Path.GetFileNameWithoutExtension(bgmPath)}.wav");
        List<string> filterChain = [];
        List<string> options = [$"-loglevel error -map 0:a -vn -acodec {meta.TargetCodec}",];

        if (needOffset)
        {
            var sec = Math.Round(meta.BgmRealOffset, 6);
            if (meta.BgmRealOffset > 0)
            {
                filterChain.Add($"adelay=delays={sec * 1000m}:all=1");
            }
            else if (meta.BgmRealOffset < 0)
            {
                filterChain.Add($"atrim=start={-sec}");
            }
        }

        if (needVoluming)
        {
            filterChain.Add($"volume={gain}dB");
        }

        if (needLimiting)
        {
            filterChain.Add($"alimiter=limit={meta.TargetMaxTruePeak}dB:attack={meta.TargetLookAheadMs}:release={meta.TargetReleaseMs}:level=0");
        }

        filterChain.Add($"aformat=sample_fmts=s16:channel_layouts=stereo:sample_rates={meta.TargetSampleRate}");

        var filterString = string.Join(",", filterChain);
        var optionString = string.Join(" ", options);

        var args = $"-i \"{bgmPath}\" {optionString} -af {filterString} -y \"{path}\"";
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = args,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var output = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var formatted = $"{process.ExitCode} - {output.Trim()}";
            throw new DiagnosticException(string.Format(Strings.Error_ffmpeg_failed, formatted));
        }
        return path;
    }

    public async static Task<string> CreateProbeTask(string inPath)
    {
        var ffprobe = new Process();
        try
        {
            ffprobe.StartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -select_streams a:0 -show_entries stream=codec_name,channels,sample_rate -show_format -of json \"{inPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            var outputBuilder = new StringBuilder();
            ffprobe.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data)) outputBuilder.AppendLine(e.Data);
            };

            ffprobe.Start();
            ffprobe.BeginOutputReadLine();
            var ffprobeErrorTask = ffprobe.StandardError.ReadToEndAsync();

            await ffprobe.WaitForExitAsync();
            var error = await ffprobeErrorTask;

            if (ffprobe.ExitCode != 0 || outputBuilder.Length == 0)
            {
                var formatted = $"{ffprobe.ExitCode} - {error.Trim()}";
                throw new DiagnosticException(string.Format(Strings.Error_ffprobe_failed, formatted));
            }
            return outputBuilder.ToString().Trim();
        }
        finally
        {
            ffprobe.Dispose();
        }
    }

    public async static Task<string> CreateEbur128Task(string inPath)
    {
        var ffmpeg = new Process();
        try
        {
            ffmpeg.StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -i \"{inPath}\" -map 0:a -vn -af ebur128@ebur128=peak=true:framelog=quiet -map_metadata -1 -f null -",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            ffmpeg.Start();
            var ffmpegErrorTask = ffmpeg.StandardError.ReadToEndAsync();

            await ffmpeg.WaitForExitAsync();
            var eburOutput = await ffmpegErrorTask;

            if (ffmpeg.ExitCode != 0)
            {
                var formatted = $"{ffmpeg.ExitCode} - {eburOutput.Trim()}";
                throw new DiagnosticException(string.Format(Strings.Error_ffmpeg_failed, formatted));
            }

            return eburOutput;
        }
        finally
        {
            ffmpeg.Dispose();
        }
    }

    public async static Task<AudioInformation> AnalyzeAudioAsync(string inPath)
    {
        var probeTask = CreateProbeTask(inPath);
        var eburTask = CreateEbur128Task(inPath);

        await Task.WhenAll(probeTask, eburTask);

        var probeOutput = await probeTask;
        var eburOutput = await eburTask;

        AudioInfo probe;
        try
        {
            probe = JsonSerializer.Deserialize<AudioInfo>(probeOutput, JsonSerializerOptions) ?? throw new DiagnosticException(string.Format(Strings.Error_ffprobe_failed, probeOutput));
            if (probe.Streams.Count == 0) throw new DiagnosticException(Strings.Error_no_audio_stream);
        }
        catch (JsonException ex)
        {
            throw new DiagnosticException(string.Format(Strings.Error_ffprobe_failed_parse, ex.Message, probeOutput));
        }

        var i = LUFS_REGEX().Match(eburOutput);
        var peak = DBFS_REGEX().Match(eburOutput);

        if (!i.Success || !peak.Success) throw new InvalidOperationException(string.Format(Strings.Error_ffmpeg_failed_parse, eburOutput));

        var loudnessValue = double.Parse(i.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var peakValue = double.Parse(peak.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

        var stream = probe.Streams[0];
        return new AudioInformation
        {
            Codec = stream.CodecName,
            ChannelCount = stream.Channels,
            SampleRate = int.Parse(stream.SampleRate, System.Globalization.CultureInfo.InvariantCulture),
            IntegratedLoudness = loudnessValue,
            TruePeak = peakValue
        };
    }

    private sealed class AudioInfo
    {
        [JsonPropertyName("streams")]
        public List<AudioStreamInfo> Streams { get; set; } = [];

        public class AudioStreamInfo
        {
            [JsonPropertyName("codec_name")]
            public string CodecName { get; set; } = string.Empty;

            [JsonPropertyName("channels")]
            public int Channels { get; set; }

            [JsonPropertyName("sample_rate")]
            public string SampleRate { get; set; } = string.Empty;
        }
    }

    [GeneratedRegex(@"I:\s*(-?\d+\.\d+) LUFS")]
    private static partial Regex LUFS_REGEX();

    [GeneratedRegex(@"Peak:\s*(-?\d+\.\d+) dBFS")]
    private static partial Regex DBFS_REGEX();

    public static bool CheckInstalled()
    {
        try
        {
            using var ffmpegProcess = new Process();
            ffmpegProcess.StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            ffmpegProcess.Start();
            var ffmpegOutput = ffmpegProcess.StandardOutput.ReadToEnd();
            ffmpegProcess.WaitForExit();

            using var ffprobeProcess = new Process();
            ffprobeProcess.StartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            ffprobeProcess.Start();
            var ffprobeOutput = ffprobeProcess.StandardOutput.ReadToEnd();
            ffprobeProcess.WaitForExit();

            return !string.IsNullOrWhiteSpace(ffmpegOutput) && !string.IsNullOrWhiteSpace(ffprobeOutput);
        }
        catch
        {
            return false;
        }
    }
}

public class AudioInformation
{
    public string Codec { get; init; } = string.Empty;
    public int ChannelCount { get; init; }
    public int SampleRate { get; init; }
    public double IntegratedLoudness { get; init; }
    public double TruePeak { get; init; }
}