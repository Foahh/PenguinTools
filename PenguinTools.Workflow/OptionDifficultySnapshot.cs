using PenguinTools.Core.Metadata;
using mgxc = PenguinTools.Chart.Models.mgxc;

namespace PenguinTools.Workflow;

public sealed record OptionDifficultySnapshot(Difficulty Difficulty, int? SongId, mgxc.Chart Chart, Meta Meta);
