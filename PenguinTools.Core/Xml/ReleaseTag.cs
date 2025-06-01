using PenguinTools.Common.Asset;
using System.Xml.Serialization;

namespace PenguinTools.Common.Xml;

[XmlRoot("ReleaseTagData")]
public sealed class ReleaseTag : XmlElement<ReleaseTag>
{
    protected override string FileName => "ReleaseTag.xml";

    internal ReleaseTag()
    {
    }

    public ReleaseTag(int id)
    {
        DataName = $"releaseTag{id:000000}";
    }

    [XmlElement("name")]
    public Entry Name { get; set; } = Entry.Default;

    [XmlElement("titleName")]
    public string TitleName { get; set; } = string.Empty;

    public static readonly ReleaseTag Default = new(20)
    {
        Name = Entry.Default,
        TitleName = "CHUNITHM 自制譜"
    };
}