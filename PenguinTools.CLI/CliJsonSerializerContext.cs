using System.Text.Json.Serialization;

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
internal sealed partial class CliJsonSerializerContext : JsonSerializerContext;
