using PenguinTools.Core.Metadata;
using umgr = PenguinTools.Chart.Models.umgr;

namespace PenguinTools.Workflow;

public sealed record OptionDifficultySnapshot(Difficulty Difficulty, int? SongId, umgr.Chart Chart, Meta Meta);