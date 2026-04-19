using System.Text.Json.Serialization;
using PenguinTools.Workflow;

namespace PenguinTools.CLI;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CliResponse))]
[JsonSerializable(typeof(CliCommandData))]
[JsonSerializable(typeof(CliChartSummary))]
[JsonSerializable(typeof(CliArtifact))]
[JsonSerializable(typeof(CliArtifact[]))]
[JsonSerializable(typeof(CliDiagnosticPayload))]
[JsonSerializable(typeof(CliDiagnosticPayload[]))]
[JsonSerializable(typeof(CliProcessPayload))]
[JsonSerializable(typeof(OptionDocument))]
[JsonSerializable(typeof(ChartFileDiscoveryMode))]
internal sealed partial class CliJsonSerializerContext : JsonSerializerContext;
