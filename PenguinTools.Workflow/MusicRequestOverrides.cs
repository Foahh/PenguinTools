namespace PenguinTools.Workflow;

public sealed record AudioRequestOverrides(
    string? DummyAcbPath,
    string? WorkingAudioPath,
    ulong? HcaEncryptionKey)
{
    public static AudioRequestOverrides Default { get; } = new(null, null, null);
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
