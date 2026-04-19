using System.CommandLine;

namespace PenguinTools.CLI;

internal sealed record AudioCommandOptions(
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

