namespace PenguinTools.Workflow;

public sealed record OptionExportSettings(
    bool ConvertChart,
    bool ConvertJacket,
    bool ConvertAudio,
    bool ConvertBackground,
    bool GenerateReleaseTagXml,
    int ReleaseTagId,
    string ReleaseTagTitleName,
    bool GenerateEventXml,
    int UltimaEventId,
    int WeEventId,
    int BatchSize,
    OptionConversionCache? ConversionCache = null);
