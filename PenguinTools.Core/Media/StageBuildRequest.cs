using PenguinTools.Core.Asset;

namespace PenguinTools.Core.Media;

public sealed record StageBuildRequest(
    AssetManager Assets,
    string BackgroundPath,
    string?[]? EffectPaths,
    int? StageId,
    string OutFolder,
    Entry NoteFieldLane);
