using PenguinTools.Core.Asset;
using PenguinTools.Core.Metadata;

namespace PenguinTools.Workflow;

public sealed record OptionBookSnapshot(
    Meta BookMeta,
    bool IsCustomStage,
    int? StageId,
    Entry NotesFieldLine,
    Entry Stage,
    string Title,
    IReadOnlyDictionary<Difficulty, OptionDifficultySnapshot> Difficulties);