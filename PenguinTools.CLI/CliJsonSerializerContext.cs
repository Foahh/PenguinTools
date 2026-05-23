using System.Text.Json.Serialization;
using PenguinTools.Core;
using PenguinTools.Workflow;

namespace PenguinTools.CLI;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CliResponse))]
[JsonSerializable(typeof(CliCommandData))]
[JsonSerializable(typeof(CliEntrySummary))]
[JsonSerializable(typeof(CliChartSummary))]
[JsonSerializable(typeof(CliScanDifficultySummary))]
[JsonSerializable(typeof(CliScanDifficultySummary[]))]
[JsonSerializable(typeof(CliScanBookSummary))]
[JsonSerializable(typeof(CliScanBookSummary[]))]
[JsonSerializable(typeof(CliScanSummary))]
[JsonSerializable(typeof(CliArtifact))]
[JsonSerializable(typeof(CliArtifact[]))]
[JsonSerializable(typeof(CliDiagnosticPayload))]
[JsonSerializable(typeof(CliDiagnosticPayload[]))]
[JsonSerializable(typeof(CliProcessPayload))]
[JsonSerializable(typeof(OptionDocument))]
[JsonSerializable(typeof(OptionConversionCache))]
[JsonSerializable(typeof(OptionConversionCacheEntry))]
[JsonSerializable(typeof(ChartFileFormat))]
[JsonSerializable(typeof(List<ChartFileFormat>))]
[JsonSerializable(typeof(ExecutionInfo), TypeInfoPropertyName = "RuntimeExecutionInfo")]
internal sealed partial class CliJsonSerializerContext : JsonSerializerContext;
