using System.Xml.Serialization;
using PenguinTools.Core.Asset;

namespace PenguinTools.Core.Xml;

[XmlRoot("ReleaseTagData")]
public class ReleaseTag : XmlElement<ReleaseTag>
{
    public const int DefaultId = 000099;
    public const string DefaultTitleName = "自制譜";

    public static readonly ReleaseTag Default = new(DefaultId)
    {
        Name = Entry.Default
    };

    internal ReleaseTag()
    {
    }

    public ReleaseTag(int id, string titleName = DefaultTitleName)
    {
        DataName = $"releaseTag{id:000000}";
        TitleName = string.IsNullOrWhiteSpace(titleName) ? DefaultTitleName : titleName.Trim();
    }

    protected override string FileName => "ReleaseTag.xml";

    [XmlElement("name")] public Entry Name { get; set; } = Entry.Default;

    [XmlElement("titleName")] public string TitleName { get; set; } = string.Empty;
}
