using System.Text.Json;
using PenguinTools.Core;

namespace PenguinTools.CLI;

internal enum CliOutputFormat
{
    Json,
    Text
}

internal sealed record CliArtifact(string Kind, string Path);

internal sealed record CliEntrySummary(
    int Id,
    string Name,
    string? Data = null);

internal sealed record CliChartSummary(
    string? MgxcId,
    int? SongId,
    string Title,
    string Difficulty,
    decimal Level);

internal sealed record CliScanDifficultySummary(
    string Difficulty,
    string? MgxcId,
    int? SongId,
    string Title,
    string Artist,
    string Designer,
    decimal Level,
    bool IsMain,
    string FilePath);

internal sealed record CliScanBookSummary(
    int? SongId,
    string Title,
    string Artist,
    string? MainDifficulty,
    bool IsCustomStage,
    int? StageId,
    CliEntrySummary NotesFieldLine,
    CliEntrySummary Stage,
    CliScanDifficultySummary[] Charts);

internal sealed record CliScanSummary(
    string DiscoveryOrder,
    int BatchSize,
    int BookCount,
    int ChartCount,
    CliScanBookSummary[] Books);

internal sealed record CliCommandData(
    string? InputPath = null,
    string? OutputPath = null,
    string? OutputDirectory = null,
    string? SourcePath = null,
    int? StageId = null,
    string? StageName = null,
    CliChartSummary? Chart = null,
    CliArtifact[]? Artifacts = null,
    CliScanSummary? Scan = null);

internal sealed record CliCommandOutcome(OperationResult Result, string? Message = null, CliCommandData? Data = null);

internal static class CliOutput
{
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

        if (string.IsNullOrWhiteSpace(outcome.Message)) return;

        var writer = outcome.Result.Succeeded ? Console.Out : Console.Error;
        writer.WriteLine(outcome.Message);
    }

    private static void WriteJson(string commandName, int exitCode, CliCommandOutcome outcome)
    {
        var response = new CliResponse(
            1,
            commandName,
            outcome.Result.Succeeded,
            exitCode,
            outcome.Message,
            outcome.Data,
            CliDiagnostics.ToPayload(outcome.Result.Diagnostics));

        Console.Out.WriteLine(JsonSerializer.Serialize(response, CliJsonSerializerContext.Default.CliResponse));
    }
}

internal sealed record CliResponse(
    int SchemaVersion,
    string Command,
    bool Success,
    int ExitCode,
    string? Message,
    CliCommandData? Data,
    CliDiagnosticPayload[] Diagnostics);

internal sealed record CliDiagnosticPayload(
    string Severity,
    string Message,
    string? Path = null,
    int? Line = null,
    int? Time = null,
    string? FormattedTime = null,
    CliProcessPayload? Process = null);

internal sealed record CliProcessPayload(
    string Command,
    int ExitCode,
    string ExitCodeName,
    string? StandardOutput = null,
    string? StandardError = null);
