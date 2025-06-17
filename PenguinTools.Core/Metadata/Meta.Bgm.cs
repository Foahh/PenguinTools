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
            return BgmManualOffset + BgmCalculatedOffset;
        }
    }

    private decimal BgmCalculatedOffset
    {
        get
        {
            var beatsPerSecond = BgmInitialBpm / 60;
            var beatLength = 1 / beatsPerSecond;
            var measureLength = beatLength * BgmInitialNumerator;
            var fractionOfMeasure = measureLength * (4m / BgmInitialDenominator);
            return fractionOfMeasure;
        }
    }

    public decimal BgmManualOffset { get; set; }

    public bool BgmEnableBarOffset { get; set; }
    public decimal BgmInitialBpm { get; set; } = 120m;
    public int BgmInitialNumerator { get; set; } = 4;
    public int BgmInitialDenominator { get; set; } = 4;

    public decimal BgmPreviewStart { get; set; }
    public decimal BgmPreviewStop { get; set; }
}