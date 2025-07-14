using PenguinTools.Core.Asset;

namespace PenguinTools.Core.Metadata;

public partial record Meta
{
    public string JacketFilePath { get; set; } = string.Empty;
    public string FullJacketFilePath => GetFullPath(JacketFilePath);

    public bool IsCustomStage { get; set; }
    public int? StageId { get; set; }

    // use "bgi" to prevent messing up with "bgm" mentally
    public string BgiFilePath { get; set; } = string.Empty;
    public string FullBgiFilePath => GetFullPath(BgiFilePath);

    public Entry NotesFieldLine { get; set; } = new(0, "Orange", "オレンジ");
    public Entry Stage { get; set; } = new(8, "レーベル 共通0008_新イエローリング");
}