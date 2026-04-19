using System.Text.Json;
using System.Text.Json.Serialization;

namespace PenguinTools.Workflow;

public sealed class OptionDocument
{
    public string OptionName { get; set; } = "AXXX";

    public bool ConvertChart { get; set; } = true;

    public ChartFileDiscoveryMode ChartFileDiscovery { get; set; } = ChartFileDiscoveryMode.MgxcFirst;

    public bool ConvertAudio { get; set; } = true;

    public bool ConvertJacket { get; set; } = true;

    public bool ConvertBackground { get; set; } = true;

    public bool GenerateEventXml { get; set; } = true;

    public bool GenerateReleaseTagXml { get; set; } = true;

    public int UltimaEventId { get; set; } = 1000001;

    public int WeEventId { get; set; } = 1000002;

    public int BatchSize { get; set; } = 8;

    public string WorkingDirectory { get; set; } = string.Empty;

    public bool HasExportableWork() =>
        ConvertChart || ConvertAudio || ConvertJacket || ConvertBackground || GenerateEventXml;

    public OptionExportSettings ToExportSettings() =>
        new(
            ConvertChart,
            ConvertJacket,
            ConvertAudio,
            ConvertBackground,
            GenerateReleaseTagXml,
            GenerateEventXml,
            UltimaEventId,
            WeEventId,
            BatchSize);
}

public static class OptionDocumentJson
{
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
