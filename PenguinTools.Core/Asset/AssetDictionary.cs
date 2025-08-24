using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace PenguinTools.Core.Asset;

public enum AssetType
{
    [JsonStringEnumMemberName("genreNames")]
    GenreNames,
    [JsonStringEnumMemberName("notesFieldLine")]
    FieldLines,
    [JsonStringEnumMemberName("stageName")]
    StageNames,
    [JsonStringEnumMemberName("worldsEndTagName")]
    WeTagNames
}

public class AssetDictionary
{
    private readonly Dictionary<AssetType, SortedSet<Entry>> _database;

    public IReadOnlySet<Entry> GenreNames => _database[AssetType.GenreNames];
    public IReadOnlySet<Entry> FieldLines => _database[AssetType.FieldLines];
    public IReadOnlySet<Entry> StageNames => _database[AssetType.StageNames];
    public IReadOnlySet<Entry> WeTagNames => _database[AssetType.WeTagNames];

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AssetDictionary()
    {
        _database = new Dictionary<AssetType, SortedSet<Entry>>();
        foreach (var type in Enum.GetValues<AssetType>()) _database[type] = [];
    }

    public AssetDictionary(string path) : this()
    {
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
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
        var dict = JsonSerializer.Deserialize<Dictionary<AssetType, SortedSet<Entry>>>(json, Options);
        if (dict == null) return;
        MergeWith(dict);
    }

    public void MergeWith(Dictionary<AssetType, SortedSet<Entry>> databases)
    {
        foreach (var (assetType, sourceSet) in databases)
        {
            _database[assetType].UnionWith(sourceSet);
        }
    }
    
    public void MergeWith(params AssetDictionary[] databases)
    {
        foreach (var db in databases)
        {
            foreach (var (assetType, sourceSet) in db._database)
            {
                _database[assetType].UnionWith(sourceSet);
            }
        }
    }

    public void SubtractWith(params AssetDictionary[] databases)
    {
        foreach (var db in databases)
        {
            foreach (var (assetType, sourceSet) in db._database)
            {
                _database[assetType].ExceptWith(sourceSet);
            }
        }
    }

    public async Task SaveAsync(string path, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(_database, Options);
        await File.WriteAllTextAsync(path, json, ct);
    }

    public void Clear()
    {
        foreach (var set in _database.Values)
        {
            set.Clear();
        }
    }

    public SortedSet<Entry> this[AssetType type]
    {
        get => _database[type];
        set => _database[type] = value;
    }

    #region Collect

    public static async Task<Dictionary<AssetType, SortedSet<Entry>>> CollectAsync(string workDir, CancellationToken ct = default)
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

    private static async Task<List<Entry>> ReadEntriesAsync(string path, string entryName, CancellationToken ct = default)
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

    private static async Task<SortedSet<Entry>> CollectOneAsync(string root, string fileName, string entryName, CancellationToken ct = default)
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

    public static async Task<Dictionary<AssetType, SortedSet<Entry>>> CollectManyAsync(string root, IEnumerable<(string FileName, string EntryName)> specs, CancellationToken ct = default)
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
        return JsonSerializer.Deserialize<Dictionary<AssetType, SortedSet<Entry>>>(json, Options) ?? [];
    }

    #endregion
}