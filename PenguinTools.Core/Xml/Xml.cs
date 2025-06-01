using PenguinTools.Common.Asset;
using System.Xml;
using System.Xml.Serialization;

namespace PenguinTools.Common.Xml;

public abstract class XmlElement<T>
{
    protected abstract string FileName { get; }

    [XmlNamespaceDeclarations]
    public XmlSerializerNamespaces Xmlns => new(
    [
        new XmlQualifiedName("xsi", XmlConstants.XmlnsXsi),
        new XmlQualifiedName("xsd", XmlConstants.XmlnsXsd)
    ]);

    [XmlElement("dataName")]
    public string DataName { get; set; } = string.Empty;

    public async Task<string> SaveDirectoryAsync(string baseFolder)
    {
        var serializer = new XmlSerializer(typeof(T));
        var folder = Path.Combine(baseFolder, DataName);
        Directory.CreateDirectory(folder);
        await using var streamWriter = new StreamWriter(Path.Combine(folder, FileName));
        serializer.Serialize(streamWriter, this);
        return folder;
    }
}

public class PathElement
{
    [XmlElement("path")]
    public string Path { get; set; } = string.Empty;

    public static implicit operator string(PathElement elem)
    {
        return elem.Path;
    }

    public static implicit operator PathElement(string value)
    {
        return new PathElement { Path = value };
    }
}

public class ValueElement
{
    [XmlElement("value")]
    public int Value { get; set; }

    public static implicit operator int(ValueElement elem)
    {
        return elem.Value;
    }

    public static implicit operator ValueElement(int value)
    {
        return new ValueElement { Value = value };
    }
}

public class EntryCollection
{
    [XmlArray("list")]
    [XmlArrayItem("StringID")]
    public List<Entry> List { get; private init; } = [];

    public static implicit operator List<Entry>(EntryCollection elem)
    {
        return elem.List;
    }

    public static implicit operator EntryCollection(List<Entry> value)
    {
        return new EntryCollection { List = value };
    }
}