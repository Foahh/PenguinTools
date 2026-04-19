namespace PenguinTools.Workflow;

public sealed record OptionExportSettings(
    bool ConvertChart,
    bool ConvertJacket,
    bool ConvertAudio,
    bool ConvertBackground,
    bool GenerateReleaseTagXml,
    bool GenerateEventXml,
    int UltimaEventId,
    int WeEventId,
    int BatchSize);