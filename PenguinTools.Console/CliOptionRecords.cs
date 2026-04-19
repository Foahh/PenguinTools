using System.CommandLine;

namespace PenguinTools.CLI;

internal sealed record MusicCommandOptions(
    Option<string?> DummyAcbPath,
    Option<string?> WorkingAudioPath,
    Option<ulong?> HcaEncryptionKey);

internal sealed record StageCommandOptions(
    Option<string?> BackgroundPath,
    Option<string?> Effect1Path,
    Option<string?> Effect2Path,
    Option<string?> Effect3Path,
    Option<string?> Effect4Path,
    Option<int?> StageId,
    Option<int?> NoteFieldLaneId,
    Option<string?> NoteFieldLaneName,
    Option<string?> NoteFieldLaneData,
    Option<string?> StageTemplatePath,
    Option<string?> NotesFieldTemplatePath);

internal sealed record MusicRequestOverrides(
    string? DummyAcbPath,
    string? WorkingAudioPath,
    ulong? HcaEncryptionKey);

internal sealed record StageRequestOverrides(
    string? BackgroundPath,
    string?[] EffectPaths,
    int? StageId,
    int? NoteFieldLaneId,
    string? NoteFieldLaneName,
    string? NoteFieldLaneData,
    string? StageTemplatePath,
    string? NotesFieldTemplatePath)
{
    public bool HasBuildInputs =>
        !string.IsNullOrWhiteSpace(BackgroundPath) ||
        StageId is not null ||
        EffectPaths.Any(path => !string.IsNullOrWhiteSpace(path));
}
