using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace PenguinTools.Common.Asset;

public class AssetDictionary : Dictionary<AssetType, SortedSet<Entry>>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AssetDictionary()
    {
        foreach (var type in Enum.GetValues<AssetType>())
        {
            if (!ContainsKey(type)) this[type] = [];
        }
    }

    public AssetDictionary(string path) : this()
    {
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
        Load(json);
    }

    public AssetDictionary(byte[] bytes) : this()
    {
        var json = Encoding.UTF8.GetString(bytes);
        Load(json);
    }

    public AssetDictionary(Stream stream) : this()
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var json = reader.ReadToEnd();
        Load(json);
    }

    private void Load(string json)
    {
        var dict = JsonSerializer.Deserialize<AssetDictionary>(json, Options);
        if (dict == null) return;
        MergeWith(dict);
    }

    public void MergeWith(params AssetDictionary[] databases)
    {
        foreach (var db in databases)
        {
            foreach (var (assetType, sourceSet) in db) this[assetType].UnionWith(sourceSet);
        }
    }

    public async Task SaveAsync(string path, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(this, Options);
        await File.WriteAllTextAsync(path, json, ct);
    }

    #region Collect

    public async static Task<AssetDictionary> CollectAsync(string workDir, CancellationToken ct = default)
    {
        var specs = new (string FileName, string EntryName)[]
        {
            ("Music.xml", "genreNames"),
            ("Music.xml", "worldsEndTagName"),
            ("Music.xml", "stageName"),
            ("Stage.xml", "notesFieldLine")
        };
        return await CollectManyAsync(workDir, specs, ct);
    }

    private async static Task<List<Entry>> ReadEntriesAsync(string path, string entryName, CancellationToken ct = default)
    {
        var entries = new List<Entry>();
        XDocument doc;

        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            doc = await XDocument.LoadAsync(stream, LoadOptions.PreserveWhitespace, ct);
        }
        catch (XmlException)
        {
            return entries;
        }
        catch (IOException)
        {
            return entries;
        }

        ct.ThrowIfCancellationRequested();

        var node = doc.Root?.Element(entryName);
        if (node is null) return entries;

        var listNode = node.Element("list");
        if (listNode != null)
        {
            var stringIdNodes = listNode.Elements("StringID");
            foreach (var stringIdNode in stringIdNodes)
            {
                ct.ThrowIfCancellationRequested();

                var id = (stringIdNode.Element("id")?.Value ?? string.Empty).Trim();
                var str = (stringIdNode.Element("str")?.Value ?? string.Empty).Trim();
                var data = (stringIdNode.Element("data")?.Value ?? string.Empty).Trim();
                if (int.TryParse(id, out var val))
                {
                    entries.Add(new Entry(val, str, data));
                }
            }
        }
        else
        {
            var id = (node.Element("id")?.Value ?? string.Empty).Trim();
            var str = (node.Element("str")?.Value ?? string.Empty).Trim();
            var data = (node.Element("data")?.Value ?? string.Empty).Trim();
            if (int.TryParse(id, out var val))
            {
                entries.Add(new Entry(val, str, data));
            }
        }

        return entries;
    }

    private async static Task<SortedSet<Entry>> CollectOneAsync(string root, string fileName, string entryName, CancellationToken ct = default)
    {
        var result = new SortedSet<Entry>();
        var walker = Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories);
        foreach (var xmlFile in walker)
        {
            ct.ThrowIfCancellationRequested();
            var entries = await ReadEntriesAsync(xmlFile, entryName, ct);
            foreach (var entry in entries) result.Add(entry);
        }
        return result;
    }

    public async static Task<AssetDictionary> CollectManyAsync(string root, IEnumerable<(string FileName, string EntryName)> specs, CancellationToken ct = default)
    {
        var aggregated = new Dictionary<string, SortedSet<Entry>>();

        foreach (var (fileName, entryName) in specs)
        {
            ct.ThrowIfCancellationRequested();
            if (!aggregated.TryGetValue(entryName, out var set))
            {
                set = [];
                aggregated[entryName] = set;
            }
            var entries = await CollectOneAsync(root, fileName, entryName, ct);
            set.UnionWith(entries);
        }

        var json = JsonSerializer.Serialize(aggregated, Options);
        return JsonSerializer.Deserialize<AssetDictionary>(json, Options) ?? [];
    }

    #endregion
}