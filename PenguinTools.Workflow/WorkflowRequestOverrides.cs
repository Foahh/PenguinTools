namespace PenguinTools.Workflow;

public sealed record MusicRequestOverrides(
    string? DummyAcbPath,
    string? WorkingAudioPath,
    ulong? HcaEncryptionKey)
{
    public static MusicRequestOverrides Default { get; } = new(null, null, null);
}

public sealed record StageRequestOverrides(
    string? BackgroundPath,
    string?[] EffectPaths,
    int? StageId,
    int? NoteFieldLaneId,
    string? NoteFieldLaneName,
    string? NoteFieldLaneData,
    string? StageTemplatePath,
    string? NotesFieldTemplatePath)
{
    public static StageRequestOverrides None { get; } = new(
        null,
        [],
        null,
        null,
        null,
        null,
        null,
        null);

    public bool HasBuildInputs =>
        !string.IsNullOrWhiteSpace(BackgroundPath) ||
        StageId is not null ||
        EffectPaths.Any(path => !string.IsNullOrWhiteSpace(path));
}
