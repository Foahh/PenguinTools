using System.Text.Json;
using System.Text.Json.Serialization;
using PenguinTools.Core;
using PenguinTools.Media;

namespace PenguinTools.CLI;

internal enum CliOutputFormat
{
    Json,
    Text
}

internal sealed record CliArtifact(string Kind, string Path);

internal sealed record CliChartSummary(
    string? MgxcId,
    int? SongId,
    string Title,
    string Difficulty,
    decimal Level);

internal sealed record CliCommandData(
    string? InputPath = null,
    string? OutputPath = null,
    string? OutputDirectory = null,
    string? AssetRoot = null,
    string? SourcePath = null,
    int? StageId = null,
    string? StageName = null,
    CliChartSummary? Chart = null,
    IReadOnlyList<CliArtifact>? Artifacts = null);

internal sealed record CliCommandOutcome(OperationResult Result, string? Message = null, object? Data = null);

internal static class CliOutput
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static void Write(string commandName, CliOutputFormat format, CliCommandOutcome outcome)
    {
        var exitCode = outcome.Result.Succeeded ? 0 : 1;
        switch (format)
        {
            case CliOutputFormat.Text:
                WriteText(outcome);
                break;
            case CliOutputFormat.Json:
            default:
                WriteJson(commandName, exitCode, outcome);
                break;
        }
    }

    private static void WriteText(CliCommandOutcome outcome)
    {
        CliDiagnostics.WriteDiagnostics(outcome.Result.Diagnostics);

        if (string.IsNullOrWhiteSpace(outcome.Message))
        {
            return;
        }

        var writer = outcome.Result.Succeeded ? Console.Out : Console.Error;
        writer.WriteLine(outcome.Message);
    }

    private static void WriteJson(string commandName, int exitCode, CliCommandOutcome outcome)
    {
        var response = new CliResponse(
            SchemaVersion: 1,
            Command: commandName,
            Success: outcome.Result.Succeeded,
            ExitCode: exitCode,
            Message: outcome.Message,
            Data: outcome.Data,
            Diagnostics: CliDiagnostics.ToPayload(outcome.Result.Diagnostics));

        Console.Out.WriteLine(JsonSerializer.Serialize(response, JsonOptions));
    }
}

internal sealed record CliResponse(
    int SchemaVersion,
    string Command,
    bool Success,
    int ExitCode,
    string? Message,
    object? Data,
    IReadOnlyList<CliDiagnosticPayload> Diagnostics);

internal sealed record CliDiagnosticPayload(
    string Severity,
    string Message,
    string? Path = null,
    int? Time = null,
    string? FormattedTime = null,
    CliProcessPayload? Process = null);

internal sealed record CliProcessPayload(
    string Command,
    int ExitCode,
    string ExitCodeName,
    string? StandardOutput = null,
    string? StandardError = null);
