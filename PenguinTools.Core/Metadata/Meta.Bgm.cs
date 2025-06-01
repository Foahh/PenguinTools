using PenguinTools.Common.Audio;
using System.Text.RegularExpressions;

namespace PenguinTools.Common.Metadata;

public partial record Meta
{
    public double TargetTargetLoudness { get; set; } = -8.0; // dBFS
    public double TargetGainTolerance { get; set; } = 0.5;
    public bool IsTpLimiting { get; set; } = true;
    public double TargetMaxTruePeak { get; set; } = -1.0; // dBTP
    public int TargetLookAheadMs { get; set; } = 10;
    public int TargetReleaseMs { get; set; } = 150;
    public string TargetCodec { get; set; } = "pcm_s16le";
    public int TargetSampleRate { get; set; } = 48000;
    public int TargetChannelCount { get; set; } = 2;

    public string BgmFilePath
    {
        get;
        set
        {
            if (field == value) return;
            if (string.IsNullOrEmpty(value) && string.IsNullOrEmpty(field)) return;
            field = value;
            BgmAnalysis = FFmpeg.AnalyzeAudioAsync(FullBgmFilePath);
        }
    } = string.Empty;

    public string FullBgmFilePath => GetFullPath(BgmFilePath);
    public Task<AudioInformation>? BgmAnalysis { get; private set; }

    public decimal BgmRealOffset
    {
        get
        {
            if (!BgmEnableBarOffset) return BgmManualOffset;
            return BgmManualOffset + CalculateOffset(BgmInitialBpm, BgmInitialTimeSignature);
        }
    }

    public decimal BgmManualOffset { get; set; }
    public decimal BgmPreviewStart { get; set; }
    public decimal BgmPreviewStop { get; set; }
    public bool BgmEnableBarOffset { get; set; }
    public decimal BgmInitialBpm { get; set; } = 120m;
    public TimeSignature BgmInitialTimeSignature { get; set; } = new();

    private static decimal CalculateOffset(decimal bpm, TimeSignature signature, int bar = 1) // in seconds
    {
        var beatsPerSecond = bpm / 60;
        var beatLength = 1 / beatsPerSecond;
        var measureLength = beatLength * signature.Numerator;
        var fractionOfMeasure = measureLength * (4m / signature.Denominator);
        var offset = bar * fractionOfMeasure;
        return offset;
    }
}

public record TimeSignature(int Tick = 0, int Numerator = 4, int Denominator = 4);