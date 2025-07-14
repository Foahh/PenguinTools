using PenguinTools.Core.Asset;
using System.Xml.Serialization;

namespace PenguinTools.Core.Xml;

[XmlRoot("ReleaseTagData")]
public class ReleaseTag : XmlElement<ReleaseTag>
{
    public static readonly ReleaseTag Default = new(20)
    {
        Name = Entry.Default,
        TitleName = "CHUNITHM 自制譜"
    };

    internal ReleaseTag()
    {
    }

    public ReleaseTag(int id)
    {
        DataName = $"releaseTag{id:000000}";
    }

    protected override string FileName => "ReleaseTag.xml";

    [XmlElement("name")]
    public Entry Name { get; set; } = Entry.Default;

    [XmlElement("titleName")]
    public string TitleName { get; set; } = string.Empty;
}